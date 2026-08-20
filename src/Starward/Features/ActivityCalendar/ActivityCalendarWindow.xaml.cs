using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

namespace Starward.Features.ActivityCalendar;

[INotifyPropertyChanged]
public sealed partial class ActivityCalendarWindow : WindowEx
{


    private readonly ILogger<ActivityCalendarWindow> _logger = AppConfig.GetLogger<ActivityCalendarWindow>();


    private readonly GameNoticeService _gameNoticeService = AppConfig.GetService<GameNoticeService>();


    /// <summary>
    /// 时间轴周数
    /// </summary>
    private const int WeekCount = 6;


    /// <summary>
    /// 时间轴起点（当前周周一的前两周，当前周为第 3 格）
    /// </summary>
    private DateTimeOffset _timelineStart;


    /// <summary>
    /// 当前时刻在时间轴中的偏移天数
    /// </summary>
    private double _nowOffsetDays;


    /// <summary>
    /// 定位线是否已完成首次动画
    /// </summary>
    private bool _lineInitialized;


    /// <summary>
    /// 当前游戏
    /// </summary>
    public GameBiz CurrentGameBiz { get; set; }


    /// <summary>
    /// 父窗口句柄，用于初始定位
    /// </summary>
    public nint ParentWindowHandle { get; set; }


    public ObservableCollection<ActivityCalendarItem> Activities { get; } = [];



    public ActivityCalendarWindow()
    {
        this.InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        InitializeWindow();
    }



    private void InitializeWindow()
    {
        try
        {
            Title = Lang.ActivityCalendar_Title;
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
            int w = (int)(960 * UIScale);
            int h = (int)(640 * UIScale);
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
                CenterInScreen(960, 640);
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
        TextBlock_Title.Text = $"{CurrentGameBiz.ToGameName()} · {Lang.ActivityCalendar_Title}";
        BuildTimeline();
        UpdateNowLinePosition(animate: false);
        await LoadActivitiesAsync();
    }




    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTitleBarDragRect();
        UpdateNowLinePosition(animate: false);
    }




    private void TimelineGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateNowLinePosition(animate: false);
    }




    private void UpdateTitleBarDragRect()
    {
        try
        {
            int w = Math.Max(0, (int)RootGrid.ActualWidth);
            AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, w, 48)]);
        }
        catch { }
    }




    /// <summary>
    /// 生成连续机械时间轴（轨道 / 周标签 / 分隔线 / 刻度）
    /// </summary>
    private void BuildTimeline()
    {
        try
        {
            // 移除旧轨道
            foreach (var child in TimelineGrid.Children.Where(c => c is FrameworkElement fe && Equals(fe.Tag, "weekRail") || (c is FrameworkElement fe2 && Equals(fe2.Tag, "weekCell"))).ToList())
            {
                TimelineGrid.Children.Remove(child);
            }

            var now = DateTimeOffset.Now;
            _timelineStart = StartOfWeek(now).AddDays(-14);
            _nowOffsetDays = (now - _timelineStart).TotalDays;

            var normalText = new SolidColorBrush(Color.FromArgb(0xFF, 0x9A, 0xA0, 0xA8));
            var accentText = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x8A, 0x4D));
            var currentWeekText = new SolidColorBrush(Color.FromArgb(0xFF, 0xF2, 0xF3, 0xF5));

            // 连续轨道（金属面板）
            var rail = new Border
            {
                Tag = "weekRail",
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3E, 0x45)),
                BorderThickness = new Thickness(1),
            };
            rail.Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(0xFF, 0x1A, 0x1D, 0x22), Offset = 0.0 },
                    new GradientStop { Color = Color.FromArgb(0xFF, 0x11, 0x13, 0x16), Offset = 1.0 },
                }
            };
            var railGrid = new Grid();
            for (int i = 0; i < WeekCount; i++)
            {
                railGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            rail.Child = railGrid;
            Grid.SetColumnSpan(rail, WeekCount);
            TimelineGrid.Children.Add(rail);

            // 周标签 + 金属分隔线
            for (int i = 0; i < WeekCount; i++)
            {
                var weekStart = _timelineStart.AddDays(7 * i);
                var weekEnd = weekStart.AddDays(6);
                bool isCurrentWeek = i == 2;

                var panel = new StackPanel
                {
                    Tag = "weekCell",
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 2,
                };
                panel.Children.Add(new TextBlock
                {
                    Text = string.Format(Lang.ActivityCalendar_Week, i + 1),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    CharacterSpacing = 120,
                    Foreground = isCurrentWeek ? accentText : normalText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                panel.Children.Add(new TextBlock
                {
                    Text = $"{weekStart:MM/dd}-{weekEnd:MM/dd}",
                    FontSize = 11,
                    CharacterSpacing = 30,
                    Foreground = isCurrentWeek ? currentWeekText : normalText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                Grid.SetColumn(panel, i);
                railGrid.Children.Add(panel);

                // 周与周之间的金属分隔线
                if (i < WeekCount - 1)
                {
                    var separator = new Rectangle
                    {
                        Tag = "weekCell",
                        Width = 1,
                        Margin = new Thickness(0, 6, 0, 6),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Fill = new SolidColorBrush(Color.FromArgb(0x40, 0x5A, 0x60, 0x68)),
                    };
                    Grid.SetColumn(separator, i);
                    railGrid.Children.Add(separator);
                }
            }

            // 底部日刻度条（一个刻度代表一天，周边界加长醒目）
            int dayCount = WeekCount * 7;
            var tickStrip = new Grid
            {
                Tag = "weekCell",
                Height = 10,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            for (int i = 0; i < dayCount; i++)
            {
                tickStrip.ColumnDefinitions.Add(new ColumnDefinition());
            }
            var baseline = new Rectangle
            {
                Height = 1,
                VerticalAlignment = VerticalAlignment.Bottom,
                Fill = new SolidColorBrush(Color.FromArgb(0x60, 0x8A, 0x8F, 0x98)),
            };
            Grid.SetColumnSpan(baseline, dayCount);
            tickStrip.Children.Add(baseline);
            for (int i = 0; i <= dayCount; i++)
            {
                bool major = i % 7 == 0;
                var tick = new Rectangle
                {
                    Width = major ? 2 : 1,
                    Height = major ? 10 : 5,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)(major ? 0xB0 : 0x85), 0x8A, 0x8F, 0x98)),
                };
                if (i == 0)
                {
                    tick.HorizontalAlignment = HorizontalAlignment.Left;
                    Grid.SetColumn(tick, 0);
                }
                else if (i == dayCount)
                {
                    tick.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetColumn(tick, dayCount - 1);
                }
                else
                {
                    tick.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetColumn(tick, i - 1);
                }
                tickStrip.Children.Add(tick);
            }
            Grid.SetColumnSpan(tickStrip, dayCount);
            railGrid.Children.Add(tickStrip);

            // 甘特图布局的时间轴起点与顶部一致
            GanttLayout.TimelineStart = _timelineStart;

            TextBlock_NowCapsule.Text = DateTime.Now.ToString("MM/dd");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build timeline");
        }
    }




    /// <summary>
    /// 更新红色定位线（胶囊/垂直线/终点圆点）的位置
    /// </summary>
    private void UpdateNowLinePosition(bool animate)
    {
        try
        {
            double contentWidth = Math.Max(0, TimelineGrid.ActualWidth - 48);
            double fraction = Math.Clamp(_nowOffsetDays / (WeekCount * 7.0), 0, 1);
            double lineX = fraction * contentWidth + 24;

            double capsuleWidth = Math.Max(56, NowCapsule.ActualWidth);
            double capsuleX = Math.Clamp(lineX - capsuleWidth / 2, 4, Math.Max(4, NowLineCanvas.ActualWidth - capsuleWidth - 4));
            double capsuleY = 8;
            double capsuleHeight = 26;
            double lineY = capsuleY + capsuleHeight + 8;
            double canvasHeight = Math.Max(0, NowLineCanvas.ActualHeight);
            double dotY = Math.Max(lineY + 4, canvasHeight - 18);
            double lineHeight = Math.Max(0, dotY - lineY);

            Canvas.SetTop(NowCapsule, capsuleY);
            Canvas.SetTop(NowLine, lineY);
            NowLine.Height = lineHeight;
            Canvas.SetTop(NowLineDot, dotY - 5);

            if (animate && !_lineInitialized)
            {
                _lineInitialized = true;
                var storyboard = new Storyboard();
                AddLineAnimation(storyboard, NowCapsule, capsuleX);
                AddLineAnimation(storyboard, NowLine, lineX - 1);
                AddLineAnimation(storyboard, NowLineDot, lineX - 5);
                storyboard.Begin();
            }
            else
            {
                Canvas.SetLeft(NowCapsule, capsuleX);
                Canvas.SetLeft(NowLine, lineX - 1);
                Canvas.SetLeft(NowLineDot, lineX - 5);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update now line position");
        }
    }




    private static void AddLineAnimation(Storyboard storyboard, FrameworkElement target, double to)
    {
        var animation = new DoubleAnimation
        {
            From = 0,
            To = to,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "(Canvas.Left)");
        storyboard.Children.Add(animation);
    }




    private static DateTimeOffset StartOfWeek(DateTimeOffset dateTime)
    {
        var local = dateTime.LocalDateTime.Date;
        int daysSinceMonday = ((int)local.DayOfWeek + 6) % 7;
        return new DateTimeOffset(local.AddDays(-daysSinceMonday));
    }




    private async Task LoadActivitiesAsync()
    {
        StackPanel_Loading.Visibility = Visibility.Visible;
        ActivityScrollViewer.Visibility = Visibility.Collapsed;
        StackPanel_Error.Visibility = Visibility.Collapsed;
        Activities.Clear();
        try
        {
            var list = await _gameNoticeService.GetActivityListAsync(CurrentGameBiz);
            var now = DateTimeOffset.Now;

            var ongoing = new List<ActivityCalendarItem>();
            var upcoming = new List<ActivityCalendarItem>();
            var ended = new List<ActivityCalendarItem>();
            foreach (var announcement in list)
            {
                var status = announcement.StartTimeOffset > now ? ActivityStatus.Upcoming
                            : announcement.EndTimeOffset < now ? ActivityStatus.Ended
                            : ActivityStatus.Ongoing;
                var item = new ActivityCalendarItem(announcement, status);
                switch (status)
                {
                    case ActivityStatus.Ongoing:
                        ongoing.Add(item);
                        break;
                    case ActivityStatus.Upcoming:
                        upcoming.Add(item);
                        break;
                    case ActivityStatus.Ended:
                        ended.Add(item);
                        break;
                }
            }

            // 进行中（按结束时间升序，快结束的在前）→ 即将开始（按开始时间升序）→ 已结束（按结束时间降序，最近结束的在前）
            foreach (var item in ongoing.OrderBy(i => i.Announcement.EndTimeOffset))
            {
                Activities.Add(item);
            }
            foreach (var item in upcoming.OrderBy(i => i.Announcement.StartTimeOffset))
            {
                Activities.Add(item);
            }
            foreach (var item in ended.OrderByDescending(i => i.Announcement.EndTimeOffset))
            {
                Activities.Add(item);
            }

            TextBlock_NoActivities.Visibility = Activities.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ActivityScrollViewer.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load activity calendar ({GameBiz})", CurrentGameBiz);
            TextBlock_Error.Text = Lang.ActivityCalendar_LoadFailed;
            StackPanel_Error.Visibility = Visibility.Visible;
        }
        finally
        {
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            NowLineCanvas.Visibility = Visibility.Visible;
            UpdateNowLinePosition(animate: true);
        }
    }




    [RelayCommand]
    private void Refresh()
    {
        _ = LoadActivitiesAsync();
    }




    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.FindName("HoverOverlay") is Border overlay)
        {
            overlay.Opacity = 1;
        }
    }




    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.FindName("HoverOverlay") is Border overlay)
        {
            overlay.Opacity = 0;
        }
    }




    private void CardRoot_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Grid grid || grid.DataContext is not ActivityCalendarItem item)
        {
            return;
        }

        // 卡片滑入动画（错峰，透明度收敛到该卡片的目标亮度）
        int index = Activities.IndexOf(item);
        var translate = new TranslateTransform { Y = 12 };
        grid.RenderTransform = translate;
        grid.Opacity = 0;
        var slideStoryboard = new Storyboard();
        var moveAnimation = new DoubleAnimation
        {
            From = 12,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            BeginTime = TimeSpan.FromMilliseconds(Math.Min(index * 30, 360)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(moveAnimation, translate);
        Storyboard.SetTargetProperty(moveAnimation, "Y");
        slideStoryboard.Children.Add(moveAnimation);
        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = item.CardOpacity,
            Duration = TimeSpan.FromMilliseconds(260),
            BeginTime = TimeSpan.FromMilliseconds(Math.Min(index * 30, 360)),
        };
        Storyboard.SetTarget(fadeAnimation, grid);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        slideStoryboard.Children.Add(fadeAnimation);
        slideStoryboard.Begin();

        // 新活动红色闪烁提示
        if (item.IsNew && grid.FindName("NewActivityDot") is Ellipse dot && dot.Tag is not Storyboard)
        {
            var blinkStoryboard = new Storyboard
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            var blinkAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.15,
                Duration = TimeSpan.FromMilliseconds(600),
            };
            Storyboard.SetTarget(blinkAnimation, dot);
            Storyboard.SetTargetProperty(blinkAnimation, "Opacity");
            blinkStoryboard.Children.Add(blinkAnimation);
            dot.Tag = blinkStoryboard;
            blinkStoryboard.Begin();
        }
    }




    private async void ActivityItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ActivityCalendarItem item)
        {
            await OpenDetailAsync(item);
        }
    }




    private async Task OpenDetailAsync(ActivityCalendarItem item)
    {
        try
        {
            if (Uri.TryCreate(item.Announcement.ContentUrl, UriKind.Absolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
            }
            else
            {
                // 详情地址不可用时，打开官方公告页
                string noticeUrl = _gameNoticeService.GetGameNoticeUrl(CurrentGameBiz);
                await Launcher.LaunchUriAsync(new Uri(noticeUrl));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open activity detail");
        }
    }




}
