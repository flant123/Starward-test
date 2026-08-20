using System.Net;
using System.Net.Http.Json;

namespace Starward.Core.GameNotice;

public class GameNoticeClient
{

    private readonly HttpClient _httpClient;



    public GameNoticeClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }




    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), GameNoticeJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
        if (responseData is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new miHoYoApiException(responseData.Retcode, responseData.Message);
        }
        return responseData.Data;
    }




    public static string GetGameNoticeUrl(GameBiz biz, long uid, string? lang = null)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        return biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => $"https://sdk.mihoyo.com/hk4e/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hk4e_global => $"https://sdk.hoyoverse.com/hk4e/announcement/index.html?announcement_version=1.37&auth_appid=announcement&bundle_id=hk4e_global&channel_id=1&game=hk4e&game_biz=hk4e_global&lang={lang}&level=60&platform=pc&region=os_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://sdk.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hkrpg_global => $"https://sdk.hoyoverse.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_cn => $"https://sdk.mihoyo.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_cn&channel_id=1&game=bh3&game_biz=bh3_cn&lang=zh-cn&level=88&platform=pc&region=android01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_global => $"https://sdk.hoyoverse.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_os&channel_id=1&game=bh3&game_biz=bh3_os&lang={lang}&level=88&platform=pc&region=overseas01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.nap_cn or GameBiz.nap_bilibili => $"https://sdk.mihoyo.com/nap/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=nap_cn&channel_id=1&font_option=light&game=nap&game_biz=nap_cn&lang={lang}&level=60&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}&version=2.33",
            GameBiz.nap_global => $"https://sdk.hoyoverse.com/nap/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=nap_global&channel_id=1&font_option=light&game=nap&game_biz=nap_global&lang={lang}&level=60&platform=pc&region=prod_gf_jp&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}&version=2.33",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
    }



    public static string GetAnnListUrl(GameBiz biz, long uid, string? lang = null)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        return biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => $"https://hk4e-ann-api.mihoyo.com/common/hk4e_cn/announcement/api/getAnnList?game=hk4e&game_biz=hk4e_cn&lang={lang}&bundle_id=hk4e_cn&platform=pc&region=cn_gf01&level=60&uid={uid}",
            GameBiz.hk4e_global => $"https://sg-hk4e-api.hoyoverse.com/common/hk4e_global/announcement/api/getAnnList?game=hk4e&game_biz=hk4e_global&lang={lang}&bundle_id=hk4e_global&platform=pc&region=os_asia&level=60&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://hkrpg-ann-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAnnList?game=hkrpg&game_biz=hkrpg_cn&lang={lang}&bundle_id=hkrpg_cn&platform=pc&region=prod_gf_cn&level=70&uid={uid}",
            GameBiz.hkrpg_global => $"https://sg-hkrpg-api.hoyoverse.com/common/hkrpg_global/announcement/api/getAnnList?game=hkrpg&game_biz=hkrpg_global&lang={lang}&bundle_id=hkrpg_global&platform=pc&region=prod_official_asia&level=1&uid={uid}",
            GameBiz.bh3_cn => $"https://ann-api.mihoyo.com/common/bh3_cn/announcement/api/getAnnList?game=bh3&game_biz=bh3_cn&lang={lang}&bundle_id=bh3_cn&platform=pc&region=android01&level=88&uid={uid}",
            GameBiz.bh3_global => $"https://sg-public-api.hoyoverse.com/common/bh3_global/announcement/api/getAnnList?game=bh3&game_biz=bh3_global&lang={lang}&bundle_id=bh3_os&platform=pc&region=overseas01&level=88&uid={uid}",
            GameBiz.nap_cn or GameBiz.nap_bilibili => $"https://announcement-api.mihoyo.com/common/nap_cn/announcement/api/getAnnList?game=nap&game_biz=nap_cn&lang={lang}&bundle_id=nap_cn&platform=pc&region=prod_gf_cn&level=60&uid={uid}",
            GameBiz.nap_global => $"https://sg-announcement-api.hoyoverse.com/common/nap_global/announcement/api/getAnnList?game=nap&game_biz=nap_global&lang={lang}&bundle_id=nap_global&platform=pc&region=prod_gf_jp&level=60&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
    }


    /// <summary>
    /// 公告详情页（getAnnContent）地址
    /// </summary>
    public static string GetAnnContentUrl(GameBiz biz, int annId, long uid, string? lang = null)
    {
        string url = GetAnnListUrl(biz, uid, lang);
        return url.Replace("getAnnList", "getAnnContent") + $"&ann_id={annId}";
    }


    /// <summary>
    /// 获取公告列表（getAnnList 接口）
    /// </summary>
    public async Task<AnnListData> GetAnnListAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        string url = GetAnnListUrl(biz, uid, lang);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<AnnListData>(request, cancellationToken);
    }


    /// <summary>
    /// 获取公告内容 HTML（getAnnContent 接口），失败时返回 null
    /// </summary>
    public async Task<string?> GetAnnContentAsync(GameBiz biz, long uid, int annId, string? lang = null, CancellationToken cancellationToken = default)
    {
        string url = GetAnnContentUrl(biz, annId, uid, lang);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var data = await CommonSendAsync<AnnContentData>(request, cancellationToken);
        return data.List.FirstOrDefault()?.Content;
    }


    /// <summary>
    /// 获取游戏活动列表（活动日历数据源），按开始时间升序排列。
    /// 优先取各游戏的活动公告类型；无独立活动类型的游戏（如星穹铁道）取全部带有效时间的公告。
    /// </summary>
    public async Task<List<GameAnnouncement>> GetActivityListAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        var data = await GetAnnListAsync(biz, uid, lang, cancellationToken);

        // 各游戏的活动公告类型 ID
        int[] activityTypeIds = biz.Game switch
        {
            "hk4e" => [1],
            "nap" => [4],
            "bh3" => [8000020],
            _ => [],
        };

        List<GameAnnouncement> items;
        if (activityTypeIds.Length > 0)
        {
            items = data.List.Where(g => activityTypeIds.Contains(g.TypeId)).SelectMany(g => g.List).ToList();
            // 活动类型下暂无数据时（如绝区零偶尔将活动归入游戏公告），回退到带活动标签的公告
            if (items.Count == 0)
            {
                items = data.List.SelectMany(g => g.List).Where(a => a.TagLabel.Contains("活动", StringComparison.Ordinal) || a.TagLabel.Contains("Event", StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        else
        {
            // 星穹铁道没有独立的活动类型，取全部公告
            items = data.List.SelectMany(g => g.List).ToList();
        }

        // 解析服务器时区的时间
        foreach (var item in items)
        {
            item.ParseTime(data.Timezone);
            item.GameBiz = biz;
            item.ContentUrl = GetAnnContentUrl(biz, item.AnnId, uid, lang);
            item.CleanHtmlText();
        }

        // 过滤没有有效起止时间的条目
        var result = items.Where(a => a.StartTimeOffset != DateTimeOffset.MinValue && a.EndTimeOffset != DateTimeOffset.MinValue).ToList();

        // 无独立活动类型的游戏（如星穹铁道），过滤长期有效的系统公告（如防沉迷声明、运营声明等）
        if (activityTypeIds.Length == 0)
        {
            result = result.Where(a => (a.EndTimeOffset - a.StartTimeOffset).TotalDays <= 180).ToList();
        }

        return result.OrderBy(a => a.StartTimeOffset).ToList();
    }


    public async Task<bool> IsNoticeAlertAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        string url = biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => $"https://hk4e-ann-api.mihoyo.com/common/hk4e_cn/announcement/api/getAlertAnn?bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&uid={uid}",
            GameBiz.hk4e_global => $"https://sg-hk4e-api.hoyoverse.com/common/hk4e_global/announcement/api/getAlertAnn?game=hk4e&game_biz=hk4e_global&lang={lang}&bundle_id=hk4e_global&channel_id=1&level=60&platform=pc&region=os_asia&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://hkrpg-ann-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAlertAnn?bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.hkrpg_global => $"https://sg-hkrpg-api.hoyoverse.com/common/hkrpg_global/announcement/api/getAlertAnn?bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&uid={uid}",
            GameBiz.bh3_cn => $"https://ann-api.mihoyo.com/common/bh3_cn/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_cn&lang={lang}&bundle_id=bh3_cn&platform=pc&region=android01&level=88&channel_id=1&uid={uid}",
            GameBiz.bh3_global => $"https://sg-public-api.hoyoverse.com/common/bh3_global/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_global&lang={lang}&bundle_id=bh3_os&platform=pc&region=overseas01&level=88&channel_id=1&uid={uid}",
            GameBiz.nap_cn or GameBiz.hk4e_bilibili => $"https://announcement-api.mihoyo.com/common/nap_cn/announcement/api/getAlertAnn?bundle_id=nap_cn&channel_id=1&game=nap&game_biz=nap_cn&lang=zh-cn&level=60&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.nap_global => $"https://sg-announcement-api.hoyoverse.com/common/nap_global/announcement/api/getAlertAnn?bundle_id=nap_global&channel_id=1&game=nap&game_biz=nap_global&lang={lang}&level=60&platform=pc&region=prod_gf_jp&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var alertAnn = await CommonSendAsync<AlertAnn>(request, cancellationToken);
        return alertAnn.Remind || alertAnn.ExtraRemind;
    }








}
