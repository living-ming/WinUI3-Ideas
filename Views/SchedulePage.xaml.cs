using IDEAs.Models;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// 
    /// </summary>
    public sealed partial class SchedulePage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<Schedule_Event> _scheduleInstance;
        public ObservableCollection<Schedule_Event> ScheduleInstance
        {
            get => _scheduleInstance;
            set
            {
                if (_scheduleInstance != value)
                {
                    _scheduleInstance = value;
                    OnPropertyChanged(nameof(ScheduleInstance));
                }
            }
        }

        public ObservableCollection<string> Schedule_Categories { get; set; } = new ObservableCollection<string>();

        public Schedule AllSchedule { get; set; }
        private string currentCategory;

        private DataService _dataService;

        public SchedulePage()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            this.DataContext = this;
            Loaded += SchedulePage_Loaded;
        }

        private async void SchedulePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadScheduleAsync();
        }

        private async Task LoadScheduleAsync()
        {
            // 加载数据
            AllSchedule = await _dataService.LoadSchedule();

            // 初始化事件集合
            ScheduleInstance = new ObservableCollection<Schedule_Event>(AllSchedule.Schedules);
            ScheduleInstance.CollectionChanged += ScheduleInstance_CollectionChanged;

            // 订阅事件属性变化
            foreach (var scheduleEvent in ScheduleInstance)
            {
                scheduleEvent.PropertyChanged += ScheduleEvent_PropertyChanged;
            }

            // 初始化类别集合
            Schedule_Categories = new ObservableCollection<string>(AllSchedule.Categories);
            Schedule_Categories.CollectionChanged += ScheduleCategories_CollectionChanged;

            ApplyFilter();
            GenerateCategoryButtons();
        }

        private void ScheduleInstance_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 处理新增项
            if (e.NewItems != null)
            {
                foreach (Schedule_Event newItem in e.NewItems)
                {
                    newItem.PropertyChanged += ScheduleEvent_PropertyChanged;
                }
            }

            // 处理移除项
            if (e.OldItems != null)
            {
                foreach (Schedule_Event oldItem in e.OldItems)
                {
                    oldItem.PropertyChanged -= ScheduleEvent_PropertyChanged;
                }
            }

            // 保存数据
            SaveData();
        }

        private void ScheduleEvent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 保存数据
            SaveData();
        }

        private void ScheduleCategories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 保存数据
            SaveData();
        }

        private void SaveData()
        {

            // 保存到文件
            _dataService.SaveToFile(AllSchedule);
        }

        private void ApplyFilter(string filterOption = null, string category = null)
        {
            // 定义今天的日期（去掉时间部分）
            var today = DateTimeOffset.Now.Date;

            IEnumerable<Schedule_Event> query = AllSchedule.Schedules;

            // 应用筛选条件
            if (!string.IsNullOrEmpty(filterOption))
            {
                switch (filterOption)
                {
                    case "今日事件":
                        // 如果事件的开始和结束时间都有值，并且其时间段包含今天，则视为今日事件
                        query = query.Where(e =>
                            e.StartTime.HasValue && e.EndTime.HasValue &&
                            e.StartTime.Value.Date <= today && e.EndTime.Value.Date >= today);
                        break;
                    case "重要":
                        query = query.Where(e => e.IsImportant);
                        break;
                    case "未完成":
                        query = query.Where(e => !e.IsCompleted);
                        break;
                    case "已完成":
                        query = query.Where(e => e.IsCompleted);
                        break;
                    case "分类":
                        if (!string.IsNullOrEmpty(category))
                        {
                            query = query.Where(e => e.Category != null && e.Category == category);
                        }
                        break;
                    default:
                        // 不筛选
                        break;
                }
            }

            // 排序顺序说明：
            // 1. 重要性 (重要的在前)
            // 2. 是否包含“今日” (如果当天介于事件的开始和结束之间，则为今日事件)
            // 3. 修改时间 (最近修改的在前)
            query = query
                .OrderByDescending(e => e.IsImportant)
                .ThenByDescending(e =>
                    e.StartTime.HasValue && e.EndTime.HasValue
                    ? (e.StartTime.Value.Date <= today && e.EndTime.Value.Date >= today)
                    : false)
                .ThenByDescending(e => e.LastModifiedTime);

            // 更新事件集合（假设 ScheduleInstance 是个 ObservableCollection 或类似的集合）
            ScheduleInstance.Clear();
            foreach (var evt in query)
            {
                ScheduleInstance.Add(evt);
            }
        }

        private void GenerateCategoryButtons()
        {
            // 清空已有的分类按钮
            FilterButtonPanel.Children.Clear();

            // 添加默认筛选按钮（如“全部”、“今日事件”等）
            // 你可以根据需要添加更多默认按钮
            var allButton = new Button
            {
                Content = "全部",
                Tag = "全部",
                Margin = new Thickness(5, 0, 0, 0)
            };
            allButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(allButton);

            var todayButton = new Button
            {
                Content = "今日事件",
                Tag = "今日事件",
                Margin = new Thickness(5, 0, 0, 0)
            };
            todayButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(todayButton);
            var uncompletedButton = new Button
            {
                Content = "未完成",
                Tag = "未完成",
                Margin = new Thickness(5, 0, 0, 0)
            };
            uncompletedButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(uncompletedButton);
            var completedButton = new Button
            {
                Content = "已完成",
                Tag = "已完成",
                Margin = new Thickness(5, 0, 0, 0)
            };
            completedButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(completedButton);


            var importantButton = new Button
            {
                Content = "重要",
                Tag = "重要",
                Margin = new Thickness(5, 0, 0, 0)
            };
            importantButton.Click += FilterButton_Click;
            FilterButtonPanel.Children.Add(importantButton);

            // 动态添加分类按钮
            // 动态添加分类按钮
            foreach (var categoryName in Schedule_Categories)
            {
                var button = new Button
                {
                    Content = categoryName,
                    Tag = categoryName,
                    Margin = new Thickness(5, 0, 0, 0),
                    // 根据需要设置按钮的样式
                };
                button.Click += CategoryButton_Click;

                // 添加右键点击事件处理器
                button.RightTapped += CategoryButton_RightClick;

                FilterButtonPanel.Children.Add(button);
            }
        }
        private void CategoryButton_RightClick(object sender, RightTappedRoutedEventArgs e)
        {
            var button = sender as Button;
            var categoryName = button.Tag as string;

            // 创建菜单
            MenuFlyout menuFlyout = new MenuFlyout();

            // 创建“仅删除分类”菜单项
            MenuFlyoutItem deleteCategoryItem = new MenuFlyoutItem
            {
                Text = "仅删除分类"
            };
            deleteCategoryItem.Click += (s, ev) => DeleteCategory(categoryName, false);

            // 创建“删除全部”菜单项
            MenuFlyoutItem deleteAllItem = new MenuFlyoutItem
            {
                Text = "删除全部"
            };
            deleteAllItem.Click += (s, ev) => DeleteCategory(categoryName, true);

            // 将菜单项添加到菜单
            menuFlyout.Items.Add(deleteCategoryItem);
            menuFlyout.Items.Add(deleteAllItem);

            // 在鼠标点击的位置显示菜单
            menuFlyout.ShowAt(button, e.GetPosition(button));

            // 防止事件继续传播
            e.Handled = true;
        }
        private async void DeleteCategory(string categoryName, bool deleteEvents)
        {
            // 弹出确认对话框
            string message = deleteEvents
                ? $"确定要删除分类“{categoryName}”及其所有事件吗？"
                : $"确定要删除分类“{categoryName}”吗？（该分类下的事件将保留）";

            // 创建 ContentDialog
            ContentDialog confirmDialog = new ContentDialog
            {
                Title = "确认删除",
                Content = message,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            // 设置 XamlRoot 属性
            confirmDialog.XamlRoot = this.Content.XamlRoot;

            // 显示对话框
            var result = await confirmDialog.ShowAsync();


            if (result == ContentDialogResult.Primary)
            {
                // 从分类列表中移除该分类
                Schedule_Categories.Remove(categoryName);
                AllSchedule.Categories.Remove(categoryName);
                if (deleteEvents)
                {
                    // 删除该分类下的所有事件
                    for (int i = AllSchedule.Schedules.Count - 1; i >= 0; i--)
                    {
                        if (AllSchedule.Schedules[i].Category == categoryName)
                        {
                            AllSchedule.Schedules.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    // 保留事件，但将事件的分类置为空或默认分类
                    foreach (var evt in AllSchedule.Schedules)
                    {
                        if (evt.Category == categoryName)
                        {
                            evt.Category = null; // 或者指定为其他默认分类，例如 "未分类"
                        }
                    }
                }

                // 重新生成分类按钮
                GenerateCategoryButtons();

                // 重新应用筛选，更新事件列表
                ApplyFilter();
            }
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var categoryName = button.Tag as string;
            if (categoryName != null)
            {
                currentCategory = categoryName;
                ApplyFilter("分类", categoryName);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string filterOption = button.Tag.ToString();
            currentCategory = null;  // 重置当前分类
            ApplyFilter(filterOption);
        }

        private void Add_Schedule_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTimeOffset.Now;
            var newEvent = new Schedule_Event
            {
                Title = "New Schedule",  // 或通过对话框获取用户输入
                                         // 将起始时间设为今天的日期，时间为 0:00
                StartTime = new DateTimeOffset(now.Date, now.Offset),
                EndTime = new DateTimeOffset(now.Date, now.Offset),
                LastModifiedTime = now,
                IsImportant = false,
                IsCompleted = false
            };
            // 如果有当前分类，设置事件的分类
            if (currentCategory != null)
            {
                newEvent.Category = currentCategory;
            }

            // 将新事件添加到所有事件集合中
            AllSchedule.Schedules.Add(newEvent);
            newEvent.PropertyChanged += ScheduleEvent_PropertyChanged;

            // 保存更新后的日程表
            SaveData();

            // 重新应用当前的筛选和排序
            if (currentCategory != null)
            {
                ApplyFilter("分类", currentCategory);
            }
            else
            {
                ApplyFilter();
            }
        }

        private async void Add_Category_Click(object sender, RoutedEventArgs e)
        {
            // 输入对话框
            TextBox inputTextBox = new TextBox()
            {
                PlaceholderText = "请输入新的类别名称"
            };

            ContentDialog dialog = new ContentDialog()
            {
                Title = "添加新类别",
                Content = inputTextBox,
                PrimaryButtonText = "确认",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string userInput = inputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(userInput))
                {
                    if (Schedule_Categories.Contains(userInput))
                    {
                        var duplicateDialog = new ContentDialog()
                        {
                            Title = "类别已存在",
                            Content = $"类别 \"{userInput}\" 已存在，请输入其他名称。",
                            CloseButtonText = "确定",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await duplicateDialog.ShowAsync();
                    }
                    else
                    {
                        Schedule_Categories.Add(userInput);
                        AllSchedule.Categories.Add(userInput);

                        // 保存数据
                        SaveData();

                        // 重新生成分类按钮
                        GenerateCategoryButtons();
                    }
                }
                else
                {
                    var emptyInputDialog = new ContentDialog()
                    {
                        Title = "输入无效",
                        Content = "类别名称不能为空，请重新输入。",
                        CloseButtonText = "确定",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyInputDialog.ShowAsync();
                }
            }
        }
        private void Event_IsImportant_Click(object sender, RoutedEventArgs e)
        {
            var eventData = (sender as FrameworkElement).DataContext as Schedule_Event;
            if (eventData != null)
            {
                eventData.IsImportant = !eventData.IsImportant;
            }
        }
        private void Event_Add_Category_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var flyout = new MenuFlyout();

                // 获取当前事件
                var eventData = button.DataContext as Schedule_Event;

                // 添加分类菜单项
                foreach (var categoryName in Schedule_Categories)
                {
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = categoryName,
                        DataContext = eventData
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
                    DataContext = eventData
                };
                removeCategoryItem.Click += RemoveCategoryMenuItem_Click;
                flyout.Items.Add(removeCategoryItem);

                // 添加“添加新分类”菜单项
                var addNewCategoryItem = new MenuFlyoutItem
                {
                    Text = "添加新分类",
                    DataContext = eventData
                };
                addNewCategoryItem.Click += AddNewCategoryMenuItem_Click;
                flyout.Items.Add(addNewCategoryItem);

                // 显示 MenuFlyout
                flyout.ShowAt(button);
            }
        }
        private void RemoveCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                // 获取当前事件
                var eventData = menuItem.DataContext as Schedule_Event;
                if (eventData != null)
                {
                    eventData.Category = null; // 或者设为 string.Empty

                    // 保存数据
                    SaveData();
                }
            }
        }

        private async void AddNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个输入对话框
            TextBox inputTextBox = new TextBox()
            {
                PlaceholderText = "请输入新的类别名称"
            };

            ContentDialog dialog = new ContentDialog()
            {
                Title = "添加新类别",
                Content = inputTextBox,
                PrimaryButtonText = "确认",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string userInput = inputTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(userInput))
                {
                    if (Schedule_Categories.Contains(userInput))
                    {
                        var duplicateDialog = new ContentDialog()
                        {
                            Title = "类别已存在",
                            Content = $"类别 \"{userInput}\" 已存在，请输入其他名称。",
                            CloseButtonText = "确定",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await duplicateDialog.ShowAsync();
                    }
                    else
                    {
                        Schedule_Categories.Add(userInput);
                        // 如果有 AllSchedule.Categories 单独管理，可以同步更新
                        AllSchedule.Categories.Add(userInput);

                        // 保存数据
                        SaveData();

                        // 将新分类关联到当前事件
                        // 获取当前事件
                        var menuItem = sender as MenuFlyoutItem;
                        var eventData = menuItem?.DataContext as Schedule_Event;
                        if (eventData != null)
                        {
                            eventData.Category = userInput;

                            // 保存数据
                            SaveData();
                        }
                        GenerateCategoryButtons();

                    }
                }
                else
                {
                    var emptyInputDialog = new ContentDialog()
                    {
                        Title = "输入无效",
                        Content = "类别名称不能为空，请重新输入。",
                        CloseButtonText = "确定",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await emptyInputDialog.ShowAsync();
                }
            }
        }

        private void CategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem != null)
            {
                var selectedCategory = menuItem.Text;

                // 获取当前事件
                var eventData = menuItem.DataContext as Schedule_Event;
                if (eventData != null)
                {
                    eventData.Category = selectedCategory;

                    // 保存数据
                    SaveData();
                }
            }
        }

        private void Event_delete_Click(object sender, RoutedEventArgs e)
        {
            var eventData = (sender as FrameworkElement).DataContext as Schedule_Event;
            if (eventData != null)
            {
                AllSchedule.Schedules.Remove(eventData);
                ScheduleInstance.Remove(eventData);
                SaveData();
                // 无需重新应用过滤器，GridView 已经绑定到 ScheduleInstance
            }
        }

        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 更新 StartTime 的标志位
        private bool _isUpdatingStartTime = false;

        // 更新 EndTime 的标志位
        private bool _isUpdatingEndTime = false;

        /// <summary>
        /// 更新 StartTime，newDate 和 newTime 为 null 表示对应部分不变。
        /// </summary>
        private void UpdateStartTime(Schedule_Event scheduleEvent, DateTimeOffset? newDate, TimeSpan? newTime)
        {
            if (_isUpdatingStartTime) return;  // 防止循环更新

            try
            {
                _isUpdatingStartTime = true;
                // 取得当前值；如果为空则使用当前时间作为基础
                DateTimeOffset current = scheduleEvent.StartTime ?? DateTimeOffset.Now;

                // 如果传入新的日期，则使用新的年、月、日；否则保留当前日期部分
                int year = newDate?.Year ?? current.Year;
                int month = newDate?.Month ?? current.Month;
                int day = newDate?.Day ?? current.Day;

                // 如果传入新的时间，则使用新的时、分、秒；否则保留当前时间部分
                int hour = newTime.HasValue ? newTime.Value.Hours : current.Hour;
                int minute = newTime.HasValue ? newTime.Value.Minutes : current.Minute;
                int second = newTime.HasValue ? newTime.Value.Seconds : current.Second;

                // 创建新的 DateTimeOffset，并保持原有的时区偏移
                DateTimeOffset newStartTime = new DateTimeOffset(year, month, day, hour, minute, second, current.Offset);

                // 只有当数值发生变化时才进行赋值更新
                if (!scheduleEvent.StartTime.HasValue || !scheduleEvent.StartTime.Value.Equals(newStartTime))
                {
                    scheduleEvent.StartTime = newStartTime;
                }
            }
            finally
            {
                _isUpdatingStartTime = false;
            }
        }

        /// <summary>
        /// 更新 EndTime，newDate 和 newTime 为 null 表示对应部分不变。
        /// </summary>
        private void UpdateEndTime(Schedule_Event scheduleEvent, DateTimeOffset? newDate, TimeSpan? newTime)
        {
            if (_isUpdatingEndTime) return;  // 防止循环更新

            try
            {
                _isUpdatingEndTime = true;
                DateTimeOffset current = scheduleEvent.EndTime ?? DateTimeOffset.Now;

                int year = newDate?.Year ?? current.Year;
                int month = newDate?.Month ?? current.Month;
                int day = newDate?.Day ?? current.Day;

                int hour = newTime.HasValue ? newTime.Value.Hours : current.Hour;
                int minute = newTime.HasValue ? newTime.Value.Minutes : current.Minute;
                int second = newTime.HasValue ? newTime.Value.Seconds : current.Second;

                DateTimeOffset newEndTime = new DateTimeOffset(year, month, day, hour, minute, second, current.Offset);

                if (!scheduleEvent.EndTime.HasValue || !scheduleEvent.EndTime.Value.Equals(newEndTime))
                {
                    scheduleEvent.EndTime = newEndTime;
                }
            }
            finally
            {
                _isUpdatingEndTime = false;
            }
        }

        /// <summary>
        /// CalendarDatePicker 的 DateChanged 事件：更新 StartTime 的日期部分
        /// </summary>
        private void StartTime_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender.DataContext is Schedule_Event scheduleEvent && sender.Date.HasValue)
            {
                // 仅传入新日期，时间部分保持不变（传入 null）
                UpdateStartTime(scheduleEvent, sender.Date, null);
            }
        }

        /// <summary>
        /// TimePicker 的 TimeChanged 事件：更新 StartTime 的时间部分
        /// </summary>
        private void StartTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (sender is TimePicker timePicker && timePicker.DataContext is Schedule_Event scheduleEvent)
            {
                // 仅传入新时间，日期部分保持不变
                UpdateStartTime(scheduleEvent, null, e.NewTime);
            }
        }

        /// <summary>
        /// CalendarDatePicker 的 DateChanged 事件：更新 EndTime 的日期部分
        /// </summary>
        private void EndTime_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender.DataContext is Schedule_Event scheduleEvent && sender.Date.HasValue)
            {
                UpdateEndTime(scheduleEvent, sender.Date, null);
            }
        }

        /// <summary>
        /// TimePicker 的 TimeChanged 事件：更新 EndTime 的时间部分
        /// </summary>
        private void EndTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (sender is TimePicker timePicker && timePicker.DataContext is Schedule_Event scheduleEvent)
            {
                UpdateEndTime(scheduleEvent, null, e.NewTime);
            }
        }
    }
}
