using IDEAs.Models;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarPage : Page
    {
        private DataService _dataService;
        // 全局保存加载到的日程数据
        private static Schedule _schedule;

        // 用于在右侧边栏显示的当前日期下的事件集合
        public ObservableCollection<Schedule_Event> FilteredEvents { get; set; } = new ObservableCollection<Schedule_Event>();
        public ObservableCollection<string> Schedule_Categories { get; set; } = new ObservableCollection<string>();
        // 当前选中的日期
        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        private bool _currentIsImportant = false;

        public CalendarPage()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            PreloadSchedule();
            // 当页面加载完成时，启动加载数据
            this.Loaded += CalendarPage_Loaded;
            Calendar.CalendarViewDayItemChanging += MyCalendarView_CalendarViewDayItemChanging;

        }
        private async void PreloadSchedule()
        {
            _schedule = await _dataService.LoadSchedule() ?? new Schedule();

        }
        private void MyCalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {

            DateTime currentDate = args.Item.Date.Date;

            bool hasImportantEvent = _schedule.Schedules.Any(ev =>
                ev.IsImportant &&
                ev.StartTime.HasValue && ev.EndTime.HasValue &&
                currentDate >= ev.StartTime.Value.Date &&
                currentDate <= ev.EndTime.Value.Date);


            if (hasImportantEvent)
            {

                string dayString = args.Item.Date.Day.ToString();

                // 尝试查找 TextBlock，其文本与当天数字匹配
                TextBlock dayTextBlock = FindDayNumberTextBlock(args.Item, dayString);

                if (dayTextBlock != null)
                {
                    dayTextBlock.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                    dayTextBlock.FontSize = dayTextBlock.FontSize * 1.5; // 增大20%

                }
            }
            else
            {
                args.Item.ClearValue(CalendarViewDayItem.BackgroundProperty);
                args.Item.ClearValue(CalendarViewDayItem.BorderBrushProperty);
                args.Item.ClearValue(CalendarViewDayItem.BorderThicknessProperty);
            }
        }
        private TextBlock FindDayNumberTextBlock(DependencyObject element, string expectedText)
        {
            if (element is TextBlock tb)
            {
                // 比较 TextBlock 的文本与预期文本
                if (tb.Text == expectedText)
                {
                    return tb;
                }
            }

            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
                var result = FindDayNumberTextBlock(child, expectedText);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
        /// <summary>
        /// 页面加载时加载日程数据，并初始化视图
        /// </summary>
        private void CalendarPage_Loaded(object sender, RoutedEventArgs e)
        {
            _selectedDate = DateTimeOffset.Now;
            SelectedDateTextBlock.Text = _selectedDate.ToString("yyyy-MM-dd");
            UpdateFilteredEvents(_selectedDate);
            Schedule_Categories = new ObservableCollection<string>(_schedule.Categories);


            // 若需要默认填充新事件控件（如开始时间默认等）
            Event_StartDatePicker.Date = _selectedDate.Date;
            Event_EndDatePicker.Date = _selectedDate.Date;


        }

        /// <summary>
        /// 当 CalendarView 的选中日期发生变化时触发
        /// </summary>
        private void Calendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (sender.SelectedDates.Count > 0)
            {
                ClearEventInput();
                _selectedDate = sender.SelectedDates[0];
                SelectedDateTextBlock.Text = _selectedDate.ToString("yyyy-MM-dd");
                UpdateFilteredEvents(_selectedDate);
                // 同时更新新事件控件中默认的开始／截止值
                Event_StartDatePicker.Date = _selectedDate.Date;
                Event_EndDatePicker.Date = _selectedDate.Date;

            }
        }

        private void UpdateFilteredEvents(DateTimeOffset date)
        {
            FilteredEvents.Clear();
            if (_schedule?.Schedules != null)
            {
                var eventsOnDate = _schedule.Schedules.Where(e =>
                    e.StartTime.HasValue && e.EndTime.HasValue &&
                    // date.Date 在 [StartTime.Date, EndTime.Date] 范围内
                    date.Date >= e.StartTime.Value.Date && date.Date <= e.EndTime.Value.Date
                );

                foreach (var ev in eventsOnDate)
                {
                    FilteredEvents.Add(ev);
                }
            }
        }

        // “确认”按钮点击：创建或更新事件
        private void Event_Confirm_Click(object sender, RoutedEventArgs e)
        {
            // 检查 GridView 是否选中了某项
            var selectedEvent = EventsGridView.SelectedItem as Schedule_Event;

            // 获取右侧输入控件中的数据
            string title = Event_TitleTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(title))
            {
                // 获取开始日期和时间
                DateTime startDate = Event_StartDatePicker.Date?.Date ?? DateTime.Now.Date;

                // 获取结束日期和时间
                DateTime endDate = Event_EndDatePicker.Date?.Date ?? startDate;
                // 获取是否完成状态
                bool isCompleted = Event_isCompleted.IsChecked == true;
                // 重要性状态通过 _currentIsImportant 记录

                if (selectedEvent != null)
                {
                    // 更新选中的事件
                    selectedEvent.Title = title;
                    selectedEvent.IsCompleted = isCompleted;
                    selectedEvent.IsImportant = _currentIsImportant;
                    selectedEvent.StartTime = startDate;
                    selectedEvent.EndTime = endDate;

                    SaveData();
                }
                else
                {
                    // 创建新事件并保存所有状态
                    var newEvent = new Schedule_Event
                    {
                        Title = title,
                        StartTime = startDate,
                        EndTime = endDate,
                        IsCompleted = isCompleted,
                        IsImportant = _currentIsImportant
                    };


                    _schedule.Schedules.Add(newEvent);
                    if (newEvent.StartTime.HasValue && newEvent.StartTime.Value.Date == _selectedDate.Date)
                    {
                        FilteredEvents.Add(newEvent);
                    }
                    SaveData();
                }

                // 清空输入控件并返回创建界面

                ClearEventInput();
            }
        }

        private void Event_back_Click(object sender, RoutedEventArgs e)
        {
            ClearEventInput();

        }

        // 清空输入控件（可根据实际需要封装）
        private void ClearEventInput()
        {
            Event_TitleTextBox.Text = "";
            Event_isCompleted.IsChecked = false;

            // 使用当前选中的日期 _selectedDate 更新日期和时间控件

            Event_StartDatePicker.Date = _selectedDate;
            Event_EndDatePicker.Date = _selectedDate;

            // 重置重要性状态
            _currentIsImportant = false;
            Event_ImportantIcon.Glyph = "\uE734"; // 显示非重要图标
            EventsGridView.SelectedItem = null; // 或者 EventsGridView.SelectedIndex = -1;

        }

        // “添加”按钮点击处理示例
        private void Event_Add_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var flyout = new MenuFlyout();

                // 获取当前选中的事件
                var selectedEvent = EventsGridView.SelectedItem as Schedule_Event;

                // 如果当前没有选中事件，则直接返回
                if (selectedEvent == null)
                {
                    return;
                }

                // 添加分类菜单项
                foreach (var categoryName in Schedule_Categories)
                {
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = categoryName,
                        DataContext = selectedEvent
                    };
                    menuItem.Click += CategoryMenuItem_Click;
                    flyout.Items.Add(menuItem);
                }

                // 添加分隔符
                flyout.Items.Add(new MenuFlyoutSeparator());

                // 添加“移除分类”菜单项
                var removeCategoryItem = new MenuFlyoutItem
                {
                    Text = "移除分类",
                    DataContext = selectedEvent
                };
                removeCategoryItem.Click += RemoveCategoryMenuItem_Click;
                flyout.Items.Add(removeCategoryItem);

                // 显示 MenuFlyout
                flyout.ShowAt(button);
            }
        }
        private void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            var ev = menuItem?.DataContext as Schedule_Event;

            if (ev != null)
            {
                ev.Category = menuItem.Text;
                SaveData();
            }
        }
        private void RemoveCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            var ev = menuItem?.DataContext as Schedule_Event;

            if (ev != null)
            {
                ev.Category = null;
                SaveData();
            }
        }


        // 重要性切换示例
        private void Event_IsImportant_Click(object sender, RoutedEventArgs e)
        {
            // 切换重要性状态（true 表示重要，false 表示非重要）
            _currentIsImportant = !_currentIsImportant;
            // 更新按钮图标：假设 "\uE734" 表示重要，"\uE735" 表示非重要
            Event_ImportantIcon.Glyph = _currentIsImportant ? "\uE735" : "\uE734";

        }

        // GridView中点击事件处理，可用于编辑或选中已有事件
        private void EventsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ev = e.ClickedItem as Schedule_Event;
            if (ev != null)
            {
                // 填充标题和描述
                Event_TitleTextBox.Text = ev.Title;

                // 设置是否完成状态
                Event_isCompleted.IsChecked = ev.IsCompleted;

                // 设置开始时间
                if (ev.StartTime.HasValue)
                {
                    Event_StartDatePicker.Date = ev.StartTime.Value.Date;
                }

                // 设置结束时间
                if (ev.EndTime.HasValue)
                {
                    Event_EndDatePicker.Date = ev.EndTime.Value.Date;
                }

                // 更新重要性状态：更新图标并记录状态
                Event_ImportantIcon.Glyph = ev.IsImportantGlyph;
                _currentIsImportant = ev.IsImportant;
            }
        }
        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // sender 是 MenuFlyoutItem，通过 DataContext 获取对应的事件对象
            var menuItem = sender as MenuFlyoutItem;
            var eventToDelete = menuItem?.DataContext as Schedule_Event;
            if (eventToDelete != null)
            {
                // 从全局日程的数据列表中删除
                _schedule.Schedules.Remove(eventToDelete);

                // 如果此事件在当前选择日期的事件集合中，则删除之（确保 UI 同步更新）
                if (FilteredEvents.Contains(eventToDelete))
                {
                    FilteredEvents.Remove(eventToDelete);
                }

                // 调用保存方法进行数据持久化
                SaveData();
                ClearEventInput();

            }
        }

        private void SaveData()
        {

            // 保存到文件
            _dataService.SaveToFile(_schedule);

        }

    }
}
