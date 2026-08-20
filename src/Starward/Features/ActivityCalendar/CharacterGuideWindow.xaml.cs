using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Frameworks;
using System;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;

namespace Starward.Features.ActivityCalendar;

/// <summary>
/// 绝区零官方养成指南（WebView2 嵌入官方网页）
/// </summary>
public sealed partial class CharacterGuideWindow : WindowEx
{


    /// <summary>
    /// 官方养成指南页面地址
    /// </summary>
    private const string GuideUrl = "https://act.mihoyo.com/zzz/gt/character-builder-h/index.html#/";


    private readonly ILogger<CharacterGuideWindow> _logger = AppConfig.GetLogger<CharacterGuideWindow>();


    /// <summary>
    /// 父窗口句柄，用于初始定位
    /// </summary>
    public nint ParentWindowHandle { get; set; }


    public CharacterGuideWindow()
    {
        this.InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        InitializeWindow();
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




    private async Task InitializeWebViewAsync()
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.ProcessFailed += (_, _) => Close();
            webview.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess)
                {
                    webview.Visibility = Visibility.Visible;
                }
            };
            webview.Source = new Uri(GuideUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize character guide");
            Close();
        }
    }




}
