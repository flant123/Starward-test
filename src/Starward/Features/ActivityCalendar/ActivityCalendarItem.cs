using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Core.GameNotice;
using Starward.Language;
using System;
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


    public string Title => Announcement.Title;


    public string Subtitle => Announcement.Subtitle;


    public string TagLabel => int.TryParse(Announcement.TagLabel, out _) ? "" : Announcement.TagLabel;


    /// <summary>
    /// 横幅图地址，没有则为 null
    /// </summary>
    public string? Banner => string.IsNullOrWhiteSpace(Announcement.Banner) ? null : Announcement.Banner;


    /// <summary>
    /// 活动时间范围（本地时间）
    /// </summary>
    public string TimeRangeText
    {
        get
        {
            var start = Announcement.StartTimeOffset.ToLocalTime();
            var end = Announcement.EndTimeOffset.ToLocalTime();
            return $"{start:yyyy-MM-dd HH:mm} ~ {end:MM-dd HH:mm}";
        }
    }


    public string StatusText => Status switch
    {
        ActivityStatus.Ongoing => Lang.ActivityCalendar_Ongoing,
        ActivityStatus.Upcoming => Lang.ActivityCalendar_Upcoming,
        ActivityStatus.Ended => Lang.ActivityCalendar_Ended,
        _ => "",
    };


    public SolidColorBrush StatusBrush => Status switch
    {
        ActivityStatus.Ongoing => GetThemeBrush("SystemFillColorSuccessBrush", Color.FromArgb(0xFF, 0x0F, 0x7B, 0x0F)),
        ActivityStatus.Upcoming => GetThemeBrush("AccentFillColorDefaultBrush", Color.FromArgb(0xFF, 0x00, 0x5F, 0xC7)),
        ActivityStatus.Ended => GetThemeBrush("TextFillColorSecondaryBrush", Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A)),
        _ => new SolidColorBrush(Colors.Gray),
    };


    private static SolidColorBrush GetThemeBrush(string key, Color fallbackColor)
    {
        if (Application.Current.Resources.TryGetValue(key, out object? value) && value is SolidColorBrush brush)
        {
            return brush;
        }
        return new SolidColorBrush(fallbackColor);
    }

}
