using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Starward.Core.GameNotice;

/// <summary>
/// 游戏公告条目（来自官方公告 API getAnnList），活动日历数据源。
/// </summary>
public class GameAnnouncement
{

    [JsonPropertyName("ann_id")]
    public int AnnId { get; set; }


    [JsonPropertyName("title")]
    public string Title { get; set; } = "";


    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = "";


    [JsonPropertyName("banner")]
    public string Banner { get; set; } = "";


    [JsonPropertyName("tag_label")]
    public string TagLabel { get; set; } = "";


    [JsonPropertyName("tag_icon")]
    public string TagIcon { get; set; } = "";


    /// <summary>
    /// 服务器本地时间，格式 yyyy-MM-dd HH:mm:ss
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = "";


    /// <summary>
    /// 服务器本地时间，格式 yyyy-MM-dd HH:mm:ss
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = "";


    [JsonPropertyName("type")]
    public int Type { get; set; }


    /// <summary>
    /// 所属游戏
    /// </summary>
    [JsonIgnore]
    public GameBiz GameBiz { get; set; }


    /// <summary>
    /// 公告详情页地址（getAnnContent）
    /// </summary>
    [JsonIgnore]
    public string ContentUrl { get; set; } = "";


    /// <summary>
    /// 开始时间（已按服务器时区转换）
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset StartTimeOffset { get; private set; }


    /// <summary>
    /// 结束时间（已按服务器时区转换）
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset EndTimeOffset { get; private set; }


    /// <summary>
    /// 清理标题/副标题中的 HTML 标签（如绝区零的公告标题带 &lt;p&gt; 包裹）
    /// </summary>
    public void CleanHtmlText()
    {
        Title = CleanHtml(Title);
        Subtitle = CleanHtml(Subtitle);
    }


    private static string CleanHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? "";
        }
        var cleaned = Regex.Replace(text, "<[^>]+>", " ");
        cleaned = WebUtility.HtmlDecode(cleaned);
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }


    /// <summary>
    /// 根据服务器时区解析时间字符串。
    /// </summary>
    /// <param name="timezoneOffsetHours">服务器时区偏移（小时），如东八区为 8</param>
    public void ParseTime(int timezoneOffsetHours)
    {
        StartTimeOffset = ParseTime(StartTime, timezoneOffsetHours);
        EndTimeOffset = ParseTime(EndTime, timezoneOffsetHours);
    }


    private static DateTimeOffset ParseTime(string? time, int offsetHours)
    {
        if (DateTime.TryParseExact(time, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.FromHours(offsetHours));
        }
        return DateTimeOffset.MinValue;
    }

}
