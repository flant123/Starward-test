using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;

namespace Starward.Features.ActivityCalendar;

/// <summary>
/// 绝区零官方养成指南（WebView2 嵌入官方网页）
/// 使用显式持久化用户数据目录，并导出/恢复全部 Cookie（含会话 Cookie），保证登录状态跨启动保留。
/// </summary>
public sealed partial class CharacterGuideWindow : WindowEx
{


    /// <summary>
    /// 官方养成指南页面地址
    /// </summary>
    private const string GuideUrl = "https://act.mihoyo.com/zzz/gt/character-builder-h/index.html#/";


    /// <summary>
    /// 需要持久化 Cookie 的站点（登录相关）
    /// </summary>
    private static readonly string[] CookieUris =
    [
        "https://act.mihoyo.com/",
        "https://mihoyo.com/",
        "https://user.mihoyo.com/",
        "https://passport-api.mihoyo.com/",
        "https://webapi.account.mihoyo.com/",
        "https://api-takumi.mihoyo.com/",
    ];


    private readonly ILogger<CharacterGuideWindow> _logger = AppConfig.GetLogger<CharacterGuideWindow>();


    /// <summary>
    /// 正在保存 Cookie（避免并发写）
    /// </summary>
    private bool _savingCookies;


    /// <summary>
    /// 父窗口句柄，用于初始定位
    /// </summary>
    public nint ParentWindowHandle { get; set; }


    public CharacterGuideWindow()
    {
        this.InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        InitializeWindow();
        Closed += CharacterGuideWindow_Closed;
    }



    private void InitializeWindow()
    {
        try
        {
            Title = Lang.ActivityCalendar_CharacterGuide;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AdaptTitleBarButtonColorToActuallTheme();
            SetIcon();
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
            }
        }
        catch { }
    }




    public new void Activate()
    {
        try
        {
            int w = (int)(1024 * UIScale);
            int h = (int)(720 * UIScale);
            AppWindow? parent = ParentWindowHandle is 0 ? null : AppWindow.GetFromWindowId(new Microsoft.UI.WindowId((ulong)ParentWindowHandle));
            if (parent is not null)
            {
                var pos = parent.Position;
                var size = parent.Size;
                int x = pos.X + Math.Max(0, (size.Width - w) / 2);
                int y = pos.Y + Math.Max(0, (size.Height - h) / 2);
                AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
            }
            else
            {
                CenterInScreen(1024, 720);
            }
        }
        catch { }
        base.Activate();
    }




    protected override nint InputSiteSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_KEYDOWN)
        {
            var key = (VirtualKey)wParam;
            if (key == VirtualKey.Escape)
            {
                Close();
                return 0;
            }
        }
        return base.InputSiteSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }




    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        await InitializeWebViewAsync();
    }




    private void CharacterGuideWindow_Closed(object sender, WindowEventArgs args)
    {
        Closed -= CharacterGuideWindow_Closed;
        // 关闭时尽力保存一次 Cookie（不阻塞，导航完成时已保存过最新状态）
        _ = SaveCookiesAsync();
    }




    private async Task InitializeWebViewAsync()
    {
        try
        {
            CoreWebView2? core = await EnsureCoreWebView2Async();
            if (core is null)
            {
                Close();
                return;
            }
            core.ProcessFailed += (_, _) => Close();
            core.NavigationCompleted += CoreWebView2_NavigationCompleted;

            // 先恢复上次保存的 Cookie / 网页存储，再导航，保证打开即为登录状态
            await RestoreCookiesAsync(core);
            await InjectStorageRestoreAsync(core);

            webview.Source = new Uri(GuideUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize character guide");
            Close();
        }
    }




    /// <summary>
    /// 初始化 WebView2（应用启动时已通过 WEBVIEW2_USER_DATA_FOLDER 指定持久化用户数据目录）
    /// </summary>
    private async Task<CoreWebView2?> EnsureCoreWebView2Async()
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureCoreWebView2Async failed.");
            return null;
        }
        return webview.CoreWebView2;
    }




    private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.IsSuccess)
        {
            webview.Visibility = Visibility.Visible;
            // 每次页面加载完成后保存一次 Cookie（可捕获登录后的最新状态）
            await SaveCookiesAsync();
        }
    }




    #region Cookie 持久化


    /// <summary>
    /// Cookie 存档路径（放在用户数据目录，不会被清理缓存的设置删除）
    /// </summary>
    private static string CookieFilePath
    {
        get
        {
            string folder = string.IsNullOrWhiteSpace(AppConfig.UserDataFolder) ? AppConfig.CacheFolder : AppConfig.UserDataFolder;
            return Path.Combine(folder, "webview-cookies.dat");
        }
    }




    /// <summary>
    /// 收集并保存 Cookie（包含会话 Cookie）
    /// </summary>
    private async Task SaveCookiesAsync()
    {
        if (_savingCookies)
        {
            return;
        }
        try
        {
            var core = webview.CoreWebView2;
            if (core is null)
            {
                return;
            }
            _savingCookies = true;
            var cookies = await CollectCookiesAsync(core);
            if (cookies.Count == 0)
            {
                return;
            }
            var sb = new StringBuilder();
            foreach (var cookie in cookies)
            {
                sb.Append(Escape(cookie.Name)).Append('\t')
                  .Append(Escape(cookie.Value)).Append('\t')
                  .Append(Escape(cookie.Domain)).Append('\t')
                  .Append(Escape(cookie.Path)).Append('\t')
                  .Append(cookie.Expires.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('\t')
                  .Append(cookie.IsSession ? '1' : '0').Append('\t')
                  .Append(cookie.IsHttpOnly ? '1' : '0').Append('\t')
                  .Append(cookie.IsSecure ? '1' : '0').Append('\t')
                  .Append(((int)cookie.SameSite).ToString()).Append('\n');
            }
            string path = CookieFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
            await SaveWebStorageAsync(core);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save webview cookies");
        }
        finally
        {
            _savingCookies = false;
        }
    }




    /// <summary>
    /// localStorage / sessionStorage 存档路径
    /// </summary>
    private static string StorageFilePath
    {
        get
        {
            string folder = string.IsNullOrWhiteSpace(AppConfig.UserDataFolder) ? AppConfig.CacheFolder : AppConfig.UserDataFolder;
            return Path.Combine(folder, "webview-storage.dat");
        }
    }




    /// <summary>
    /// 保存页面的 localStorage / sessionStorage（登录令牌常存放于此）
    /// </summary>
    private static async Task SaveWebStorageAsync(CoreWebView2 core)
    {
        try
        {
            string? local = await ExecuteJsonAsync(core, "Object.fromEntries(Object.entries(localStorage))");
            string? session = await ExecuteJsonAsync(core, "Object.fromEntries(Object.entries(sessionStorage))");
            if (string.IsNullOrWhiteSpace(local) && string.IsNullOrWhiteSpace(session))
            {
                return;
            }
            string text = (local ?? "{}") + "\n" + (session ?? "{}");
            string path = StorageFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, text, Encoding.UTF8);
        }
        catch { }
    }




    private static async Task<string?> ExecuteJsonAsync(CoreWebView2 core, string script)
    {
        try
        {
            string result = await core.ExecuteScriptAsync(script);
            return string.IsNullOrWhiteSpace(result) || result == "null" ? null : result;
        }
        catch
        {
            return null;
        }
    }




    /// <summary>
    /// 在下一次导航前注入脚本，把上次保存的 localStorage / sessionStorage 写回（页面脚本执行前生效）
    /// </summary>
    private static async Task InjectStorageRestoreAsync(CoreWebView2 core)
    {
        try
        {
            string path = StorageFilePath;
            if (!File.Exists(path))
            {
                return;
            }
            string[] lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
            string local = lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]) ? lines[0] : "{}";
            string session = lines.Length > 1 && !string.IsNullOrWhiteSpace(lines[1]) ? lines[1] : "{}";
            string script = $$"""
                (function () {
                    try {
                        if (location.hostname.indexOf('mihoyo') < 0) { return; }
                    } catch (e) { return; }
                    try {
                        var ls = {{local}};
                        for (var k in ls) { try { if (localStorage.getItem(k) === null) { localStorage.setItem(k, ls[k]); } } catch (e) { } }
                    } catch (e) { }
                    try {
                        var ss = {{session}};
                        for (var k in ss) { try { if (sessionStorage.getItem(k) === null) { sessionStorage.setItem(k, ss[k]); } } catch (e) { } }
                    } catch (e) { }
                })();
                """;
            await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }
        catch { }
    }




    private static async Task<List<CoreWebView2Cookie>> CollectCookiesAsync(CoreWebView2 core)
    {
        var result = new List<CoreWebView2Cookie>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string uri in CookieUris)
        {
            IReadOnlyList<CoreWebView2Cookie> cookies;
            try
            {
                cookies = await core.CookieManager.GetCookiesAsync(uri);
            }
            catch
            {
                continue;
            }
            foreach (var cookie in cookies)
            {
                string key = $"{cookie.Name}\t{cookie.Domain}\t{cookie.Path}";
                if (keys.Add(key))
                {
                    result.Add(cookie);
                }
            }
        }
        return result;
    }




    /// <summary>
    /// 恢复上次保存的 Cookie
    /// </summary>
    private async Task RestoreCookiesAsync(CoreWebView2 core)
    {
        try
        {
            string path = CookieFilePath;
            if (!File.Exists(path))
            {
                return;
            }
            string[] lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
            var manager = core.CookieManager;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                string[] parts = line.Split('\t');
                if (parts.Length < 9)
                {
                    continue;
                }
                try
                {
                    string name = Unescape(parts[0]);
                    string value = Unescape(parts[1]);
                    string domain = Unescape(parts[2]);
                    string cookiePath = Unescape(parts[3]);
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(domain))
                    {
                        continue;
                    }
                    var cookie = manager.CreateCookie(name, value, domain, cookiePath);
                    bool isSession = parts[5] == "1";
                    // Expires 为 Unix 秒（double），-1 表示会话 Cookie；会话 Cookie 不设置过期时间以保持会话语义
                    if (!isSession && double.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double expires) && expires > 0)
                    {
                        cookie.Expires = expires;
                    }
                    cookie.IsHttpOnly = parts[6] == "1";
                    cookie.IsSecure = parts[7] == "1";
                    if (int.TryParse(parts[8], out int sameSite))
                    {
                        cookie.SameSite = (CoreWebView2CookieSameSiteKind)sameSite;
                    }
                    manager.AddOrUpdateCookie(cookie);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore webview cookies");
        }
    }




    private static string Escape(string? value) => Uri.EscapeDataString(value ?? "");


    private static string Unescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }


    #endregion


}
