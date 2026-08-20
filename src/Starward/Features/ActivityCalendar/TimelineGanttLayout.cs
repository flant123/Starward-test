using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace Starward.Features.ActivityCalendar;

/// <summary>
/// 甘特图式布局：活动条按开始时间在时间轴上定位，按持续时间定宽，纵向堆叠。
/// 使用与顶部时间轴完全一致的坐标系（左内边距 24，内容宽 = TimelineWidth - 48）。
/// </summary>
public class TimelineGanttLayout : VirtualizingLayout
{

    /// <summary>
    /// 时间轴跨度（天），与顶部 6 周时间轴一致
    /// </summary>
    public const double DaySpan = 42;


    /// <summary>
    /// 时间轴左右内边距（与 TimelineGrid 的 Padding 一致）
    /// </summary>
    public const double TimelinePadding = 24;


    /// <summary>
    /// 行高
    /// </summary>
    public const double RowHeight = 60;


    /// <summary>
    /// 行间距
    /// </summary>
    public const double RowSpacing = 8;


    /// <summary>
    /// 最短可见宽度（极短活动也保留一小段）
    /// </summary>
    private const double MinBarWidth = 8;


    /// <summary>
    /// 时间轴起点（与顶部时间轴一致）
    /// </summary>
    public DateTimeOffset TimelineStart { get; set; }


    /// <summary>
    /// 顶部时间轴的总宽度（含内边距），由窗口设置，保证条与时间轴/定位线坐标系一致
    /// </summary>
    public double TimelineWidth { get; set; }


    /// <summary>
    /// 计算活动条的水平位置与宽度（与顶部时间轴同一坐标系）
    /// </summary>
    private (double Left, double Width) GetBarRect(ActivityCalendarItem item, double availableWidth)
    {
        double totalWidth = TimelineWidth > 0 ? TimelineWidth : availableWidth;
        double contentWidth = Math.Max(0, totalWidth - TimelinePadding * 2);
        double left = TimelinePadding;
        double barWidth = contentWidth;
        if (TimelineStart != default)
        {
            double startRatio = (item.Announcement.StartTimeOffset - TimelineStart).TotalDays / DaySpan;
            double endRatio = (item.Announcement.EndTimeOffset - TimelineStart).TotalDays / DaySpan;
            left = TimelinePadding + Math.Clamp(startRatio, 0, 1) * contentWidth;
            barWidth = Math.Max(MinBarWidth, Math.Clamp(endRatio - startRatio, 0, 1) * contentWidth);
            if (left + barWidth > TimelinePadding + contentWidth)
            {
                barWidth = Math.Max(MinBarWidth, TimelinePadding + contentWidth - left);
            }
        }
        return (left, barWidth);
    }


    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        int count = context.ItemCount;
        double totalHeight = count > 0 ? count * RowHeight + (count - 1) * RowSpacing : 0;
        double measureWidth = TimelineWidth > 0 ? Math.Max(TimelineWidth, availableSize.Width) : availableSize.Width;
        for (int i = 0; i < count; i++)
        {
            var element = context.GetOrCreateElementAt(i);
            // 按实际条宽测量，避免窄条内容溢出
            double barWidth = measureWidth;
            if (context.GetItemAt(i) is ActivityCalendarItem item)
            {
                barWidth = GetBarRect(item, measureWidth).Width;
            }
            element.Measure(new Size(Math.Max(0, barWidth), RowHeight));
        }
        return new Size(Math.Max(0, measureWidth), totalHeight);
    }


    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        int count = context.ItemCount;
        for (int i = 0; i < count; i++)
        {
            var element = context.GetOrCreateElementAt(i);
            var (left, barWidth) = GetBarRect(context.GetItemAt(i) as ActivityCalendarItem, Math.Max(0, finalSize.Width));
            double y = i * (RowHeight + RowSpacing);
            element.Arrange(new Rect(left, y, barWidth, RowHeight));
        }
        return finalSize;
    }

}
