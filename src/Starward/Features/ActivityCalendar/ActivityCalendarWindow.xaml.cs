using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;

namespace Starward.Features.ActivityCalendar;

[INotifyPropertyChanged]
public sealed partial class ActivityCalendarWindow : WindowEx
{


    private readonly ILogger<ActivityCalendarWindow> _logger = AppConfig.GetLogger<ActivityCalendarWindow>();


    private readonly GameNoticeService _gameNoticeService = AppConfig.GetService<GameNoticeService>();


    /// <summary>
    /// 当前游戏
    /// </summary>
    public GameBiz CurrentGameBiz { get; set; }


    /// <summary>
    /// 父窗口句柄，用于初始定位
    /// </summary>
    public nint ParentWindowHandle { get; set; }


    public ObservableCollection<ActivityCalendarItem> OngoingActivities { get; } = [];


    public ObservableCollection<ActivityCalendarItem> UpcomingActivities { get; } = [];


    public ObservableCollection<ActivityCalendarItem> EndedActivities { get; } = [];



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
            AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, 0, 0)]);
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
            AppWindow? parent = ParentWindowHandle is 0 ? null : AppWindow.GetFromWindowId(new WindowId((ulong)ParentWindowHandle));
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
        await LoadActivitiesAsync();
    }




    private async Task LoadActivitiesAsync()
    {
        StackPanel_Loading.Visibility = Visibility.Visible;
        Pivot_Activities.Visibility = Visibility.Collapsed;
        StackPanel_Error.Visibility = Visibility.Collapsed;
        OngoingActivities.Clear();
        UpcomingActivities.Clear();
        EndedActivities.Clear();
        try
        {
            var list = await _gameNoticeService.GetActivityListAsync(CurrentGameBiz);
            var now = DateTimeOffset.Now;
            foreach (var announcement in list)
            {
                var status = announcement.StartTimeOffset > now ? ActivityStatus.Upcoming
                            : announcement.EndTimeOffset < now ? ActivityStatus.Ended
                            : ActivityStatus.Ongoing;
                var item = new ActivityCalendarItem(announcement, status);
                switch (status)
                {
                    case ActivityStatus.Ongoing:
                        OngoingActivities.Add(item);
                        break;
                    case ActivityStatus.Upcoming:
                        UpcomingActivities.Add(item);
                        break;
                    case ActivityStatus.Ended:
                        EndedActivities.Add(item);
                        break;
                }
            }
            UpdateEmptyStates();
            Pivot_Activities.Visibility = Visibility.Visible;
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
        }
    }




    private void UpdateEmptyStates()
    {
        TextBlock_NoOngoing.Visibility = OngoingActivities.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TextBlock_NoUpcoming.Visibility = UpcomingActivities.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TextBlock_NoEnded.Visibility = EndedActivities.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }




    [RelayCommand]
    private void Refresh()
    {
        _ = LoadActivitiesAsync();
    }




    private async void ActivityItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element && element.DataContext is ActivityCalendarItem item)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open activity detail");
        }
    }




}
