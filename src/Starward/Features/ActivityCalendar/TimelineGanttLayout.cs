using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace Starward.Features.ActivityCalendar;

/// <summary>
/// 甘特图式布局：活动条按开始时间在时间轴上定位，按持续时间定宽，纵向堆叠。
/// </summary>
public class TimelineGanttLayout : VirtualizingLayout
{

    /// <summary>
    /// 时间轴跨度（天），与顶部 6 周时间轴一致
    /// </summary>
    public const double DaySpan = 42;


    /// <summary>
    /// 行高
    /// </summary>
    public const double RowHeight = 52;


    /// <summary>
    /// 行间距
    /// </summary>
    public const double RowSpacing = 8;


    /// <summary>
    /// 最短可见宽度（极短活动也保留一小段）
    /// </summary>
    private const double MinBarWidth = 6;


    /// <summary>
    /// 时间轴起点（与顶部时间轴一致）
    /// </summary>
    public DateTimeOffset TimelineStart { get; set; }


    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        int count = context.ItemCount;
        double totalHeight = count > 0 ? count * RowHeight + (count - 1) * RowSpacing : 0;
        for (int i = 0; i < count; i++)
        {
            var element = context.GetOrCreateElementAt(i);
            element.Measure(new Size(Math.Max(0, availableSize.Width), RowHeight));
        }
        return new Size(Math.Max(0, availableSize.Width), totalHeight);
    }


    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        int count = context.ItemCount;
        double width = Math.Max(0, finalSize.Width);
        for (int i = 0; i < count; i++)
        {
            var element = context.GetOrCreateElementAt(i);
            double left = 0;
            double barWidth = width;
            if (context.GetItemAt(i) is ActivityCalendarItem item && TimelineStart != default)
            {
                double startRatio = (item.Announcement.StartTimeOffset - TimelineStart).TotalDays / DaySpan;
                double endRatio = (item.Announcement.EndTimeOffset - TimelineStart).TotalDays / DaySpan;
                left = Math.Clamp(startRatio, 0, 1) * width;
                barWidth = Math.Max(MinBarWidth, Math.Clamp(endRatio - startRatio, 0, 1) * width);
                if (left + barWidth > width)
                {
                    barWidth = Math.Max(MinBarWidth, width - left);
                }
            }
            double y = i * (RowHeight + RowSpacing);
            element.Arrange(new Rect(left, y, barWidth, RowHeight));
        }
        return finalSize;
    }

}
