using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Core.GameNotice;
using Starward.Language;
using System;
using System.Collections.ObjectModel;
using Windows.UI;

namespace Starward.Features.ActivityCalendar;

/// <summary>
/// 活动状态
/// </summary>
public enum ActivityStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    Ongoing,

    /// <summary>
    /// 即将开始
    /// </summary>
    Upcoming,

    /// <summary>
    /// 已结束
    /// </summary>
    Ended,
}


/// <summary>
/// 活动日历 UI 条目
/// </summary>
public class ActivityCalendarItem
{

    public GameAnnouncement Announcement { get; }


    public ActivityStatus Status { get; }


    public ActivityCalendarItem(GameAnnouncement announcement, ActivityStatus status)
    {
        Announcement = announcement;
        Status = status;
    }


    /// <summary>
    /// 活动详情中的资源图片（异步加载）
    /// </summary>
    public ObservableCollection<string> RewardImageUrls { get; } = [];


    /// <summary>
    /// 是否已请求过资源图片
    /// </summary>
    internal bool RewardImagesRequested { get; set; }


    /// <summary>
    /// 是否祈愿/调频类活动（祈愿、调频、跃迁、补给），置顶显示
    /// </summary>
    public bool IsGacha => Announcement.Title.Contains("祈愿", StringComparison.Ordinal)
                        || Announcement.Title.Contains("调频", StringComparison.Ordinal)
                        || Announcement.Title.Contains("跃迁", StringComparison.Ordinal)
                        || Announcement.Title.Contains("补给", StringComparison.Ordinal);


    public string Title => Announcement.Title;


    /// <summary>
    /// 简短短标题：只保留「」或‘’内的内容（含符号），避免长标题在窄条上显示不全
    /// </summary>
    public string ShortTitle
    {
        get
        {
            var match = System.Text.RegularExpressions.Regex.Match(Title, "「[^」]*」|‘[^’]*’");
            return match.Success ? match.Value : Title;
        }
    }


    public string Subtitle => Announcement.Subtitle;


    public string TagLabel => int.TryParse(Announcement.TagLabel, out _) ? "" : Announcement.TagLabel;


    /// <summary>
    /// 横幅图地址，没有则为 null
    /// </summary>
    public string? Banner => string.IsNullOrWhiteSpace(Announcement.Banner) ? null : Announcement.Banner;


    /// <summary>
    /// 活动时间范围（本地时间，紧凑格式）
    /// </summary>
    public string TimeRangeText
    {
        get
        {
            var start = Announcement.StartTimeOffset.ToLocalTime();
            var end = Announcement.EndTimeOffset.ToLocalTime();
            return $"{start:MM/dd HH:mm} ~ {end:MM/dd HH:mm}";
        }
    }


    public string StatusText => Status switch
    {
        ActivityStatus.Ongoing => Lang.ActivityCalendar_Ongoing,
        ActivityStatus.Upcoming => Lang.ActivityCalendar_Upcoming,
        ActivityStatus.Ended => Lang.ActivityCalendar_Ended,
        _ => "",
    };


    /// <summary>
    /// 悬停提示（完整信息，窄条时仍有详情）
    /// </summary>
    public string TooltipText => $"{Title}\n{TimeRangeText}\n{StatusText} · {RemainingText}";


    /// <summary>
    /// 剩余/开启时间提示，如「剩余 5 天」「3 天后开启」
    /// </summary>
    public string RemainingText
    {
        get
        {
            var now = DateTimeOffset.Now;
            switch (Status)
            {
                case ActivityStatus.Ongoing:
                    double daysLeft = (Announcement.EndTimeOffset - now).TotalDays;
                    return daysLeft <= 1 ? Lang.ActivityCalendar_EndsToday : string.Format(Lang.ActivityCalendar_EndsInDays, (int)Math.Ceiling(daysLeft));
                case ActivityStatus.Upcoming:
                    double daysToStart = (Announcement.StartTimeOffset - now).TotalDays;
                    return daysToStart <= 1 ? Lang.ActivityCalendar_StartsToday : string.Format(Lang.ActivityCalendar_StartsInDays, (int)Math.Ceiling(daysToStart));
                default:
                    return Lang.ActivityCalendar_Ended;
            }
        }
    }


    /// <summary>
    /// 活动条基底（深色金属，橙色仅作强调）
    /// </summary>
    public Brush CardBrush => Status switch
    {
        ActivityStatus.Ongoing => OngoingCardBrush,
        ActivityStatus.Upcoming => UpcomingCardBrush,
        ActivityStatus.Ended => EndedCardBrush,
        _ => UpcomingCardBrush,
    };


    /// <summary>
    /// 活动条左侧强调条（进行中为橙红渐变）
    /// </summary>
    public Brush AccentStripBrush => Status switch
    {
        ActivityStatus.Ongoing => OngoingAccentBrush,
        ActivityStatus.Upcoming => UpcomingAccentBrush,
        ActivityStatus.Ended => EndedAccentBrush,
        _ => UpcomingAccentBrush,
    };


    /// <summary>
    /// 活动条金属边框（进行中带橙色微光）
    /// </summary>
    public Brush CardBorderBrush => Status switch
    {
        ActivityStatus.Ongoing => OngoingCardBorderBrush,
        ActivityStatus.Upcoming => UpcomingCardBorderBrush,
        ActivityStatus.Ended => EndedCardBorderBrush,
        _ => UpcomingCardBorderBrush,
    };


    /// <summary>
    /// 已结束的活动降低亮度
    /// </summary>
    public bool IsDimmed => Status == ActivityStatus.Ended;


    /// <summary>
    /// 卡片透明度（已结束的活动半透明）
    /// </summary>
    public double CardOpacity => IsDimmed ? 0.55 : 1.0;


    /// <summary>
    /// 已结束的活动显示完成勾选
    /// </summary>
    public bool ShowCheckmark => Status == ActivityStatus.Ended;


    /// <summary>
    /// 进行中的活动显示黄色状态点
    /// </summary>
    public bool ShowStatusDot => Status == ActivityStatus.Ongoing;


    /// <summary>
    /// 状态点颜色（与状态文字一致）
    /// </summary>
    public Brush StatusDotBrush => StatusBrush;


    /// <summary>
    /// 新活动（标签为 NEW 或 24 小时内开始）显示红色闪烁提示
    /// </summary>
    public bool IsNew
    {
        get
        {
            if (Announcement.TagLabel.Contains("NEW", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return Announcement.StartTimeOffset > DateTimeOffset.Now.AddDays(-1);
        }
    }


    /// <summary>
    /// 状态文字颜色：进行中黄色、即将开始灰色、已结束暗灰
    /// </summary>
    public Brush StatusBrush => Status switch
    {
        ActivityStatus.Ongoing => new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xC9, 0x3D)),
        ActivityStatus.Upcoming => new SolidColorBrush(Color.FromArgb(0xFF, 0x9A, 0xA0, 0xA8)),
        ActivityStatus.Ended => new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0x7A, 0x7A)),
        _ => new SolidColorBrush(Colors.Gray),
    };


    // ---- 静态画刷 ----

    // 活动条基底：深色金属
    private static readonly Brush OngoingCardBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(0xFF, 0x22, 0x15, 0x10), Offset = 0.0 },
            new GradientStop { Color = Color.FromArgb(0xFF, 0x2A, 0x1B, 0x13), Offset = 1.0 },
        }
    };


    private static readonly Brush UpcomingCardBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(0xFF, 0x15, 0x18, 0x1C), Offset = 0.0 },
            new GradientStop { Color = Color.FromArgb(0xFF, 0x1B, 0x1F, 0x24), Offset = 1.0 },
        }
    };


    private static readonly Brush EndedCardBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(1, 1),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(0xFF, 0x13, 0x15, 0x17), Offset = 0.0 },
            new GradientStop { Color = Color.FromArgb(0xFF, 0x17, 0x19, 0x1B), Offset = 1.0 },
        }
    };


    // 左侧强调条
    private static readonly Brush OngoingAccentBrush = new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0, 0),
        EndPoint = new Windows.Foundation.Point(0, 1),
        GradientStops =
        {
            new GradientStop { Color = Color.FromArgb(0xFF, 0xFF, 0x8A, 0x2A), Offset = 0.0 },
            new GradientStop { Color = Color.FromArgb(0xFF, 0xB2, 0x3A, 0x12), Offset = 1.0 },
        }
    };


    private static readonly Brush UpcomingAccentBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x4A, 0x4F, 0x56));


    private static readonly Brush EndedAccentBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x36, 0x33, 0x2E));


    // 边框
    private static readonly Brush OngoingCardBorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0x7A, 0x2A));


    private static readonly Brush UpcomingCardBorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3E, 0x45));


    private static readonly Brush EndedCardBorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x30, 0x2B));

}
