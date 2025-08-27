using IDEAs.Models;
using IDEAs.Services;
using IDEAs.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DataService _dataService;
        public Item SelectedItem { get; set; } // 定义选中的项
        public ObservableCollection<Item> SelectedItems { get; } = new ObservableCollection<Item>();
        public MainViewModel ViewModel { get; set; }

        public MainPage()
        {
            InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            ViewModel = new MainViewModel
            {
                FolderSelectionDialog = ShowFolderSelectionDialog
            };
            ContentFrame.Navigate(typeof(CalendarPage));//页面
            DataContext = ViewModel;
            var selector = (ItemTemplateSelector)Resources["ItemTemplateSelector"];

            // 手动注入四个 DataTemplate
            selector.FolderTemplate = (DataTemplate)Resources["FolderTemplate"];
            selector.NoteTemplate = (DataTemplate)Resources["NoteTemplate"];
            selector.ScheduleTemplate = (DataTemplate)Resources["ScheduleTemplate"];
            selector.CalendarTemplate = (DataTemplate)Resources["CalendarTemplate"];

        }
#nullable enable
        public NotePage? GetCurrentNotePage()
        {
            return ContentFrame?.Content as NotePage;
        }

        private void GoNote(object sender, PointerRoutedEventArgs e)
        {
            // 获取当前 Note 数据
            var grid = sender as FrameworkElement;
            if (grid?.DataContext is Note note)
            {
                // 跳转并传递 Note 的 Name
                // MainPage 中导航
                ContentFrame.Navigate(typeof(NotePage), note);
            }
        }
        private void GoCalendar(object sender, RoutedEventArgs e)
        {
            // 获取被点击的项的 DataContext
            if (sender is FrameworkElement element && element.DataContext is Calendar selectedCalendar)
            {
                ContentFrame.Navigate(typeof(CalendarPage), selectedCalendar);
            }
        }
        private void GoSchedule(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Schedule selectedSchedule)
            {
                ContentFrame.Navigate(typeof(SchedulePage), selectedSchedule);

            }
        }



        private async Task<Folder?> ShowFolderSelectionDialog(List<Folder> allFolders)
        {
            var listView = new ListView
            {
                ItemsSource = allFolders,
                DisplayMemberPath = "Title",
                SelectionMode = ListViewSelectionMode.Single
            };

            var dialog = new ContentDialog
            {
                Title = "选择文件夹",
                PrimaryButtonText = "确认",
                SecondaryButtonText = "取消",
                Content = listView,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return listView.SelectedItem as Folder;
            }

            return null;
        }
        private void ShowAllFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    var item = menuFlyoutItem.DataContext as Item;
                    if (item != null)
                    {
                        viewModel._selectedItem = item;
                        viewModel.ShowAllFoldersCommand.Execute(null);
                    }
                    else
                    {
                        Debug.WriteLine("DataContext 中未找到有效的 Item 对象");
                    }
                }
            }
            else
            {
                Debug.WriteLine("MenuFlyoutItem 未找到");
            }
        }

        // 按钮事件里改为调用这个递归方法
        private async void DeleteFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe && fe.DataContext is Folder folder))
                return;

            // 显示确认对话框
            var confirm = new ContentDialog
            {
                Title = "确认删除？",
                Content = $"确定要删除笔记 “{folder.Title}” 吗？",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                XamlRoot = this.Content.XamlRoot
            };

            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                return;

            // 调用 ViewModel 的删除方法
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.DeleteFolderAsync(folder);
            }
        }


        private async void DeleteNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe && fe.DataContext is Note note))
                return;

            var confirm = new ContentDialog
            {
                Title = "确认删除？",
                Content = $"确定要删除笔记 “{note.Title}” 吗？",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                XamlRoot = this.Content.XamlRoot
            };

            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                return;
            ((App)Application.Current).DeletedNoteNames.Add(note.Name);

            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(_dataService._dataFilePath);

                string fileName = note.Name + ".json"; // ✅ 加上扩展名
                var file = await folder.GetFileAsync(fileName);
                await file.DeleteAsync();

                Debug.WriteLine($"✅ 文件 {fileName} 删除成功");
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("❌ 文件不存在，可能已被提前删除。");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 删除失败：{ex}");
            }
            if (this.DataContext is MainViewModel vm && vm.Items.Contains(note))
            {
                vm.Items.Remove(note); // 立即刷新 ListView UI

            }

        }



        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // 检测指针位置,鼠标
            var position = e.GetCurrentPoint(this).Position;
            if (position.Y <= 10) // 判断是否位于顶部50像素区域
            {
                // 显示标题栏
                var mainWindow = (MainWindow)App.m_window;
                if (mainWindow != null && mainWindow.isFullScreen)
                {
                    mainWindow.ShowTitleBar();
                }
            }
            else
            {
                // 隐藏标题栏
                var mainWindow = (MainWindow)App.m_window;
                if (mainWindow != null && mainWindow.isFullScreen)
                {
                    mainWindow.HideTitleBar();
                }
            }
        }



        //用于文件夹的隐藏显示
        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Grid;
            var stackPanel = grid?.Parent as StackPanel;

            if (stackPanel != null)
            {
                FolderListView_Tapped(stackPanel, null);
            }
        }

        private void FolderListView_Tapped(object sender, TappedRoutedEventArgs? e)
        {
            var stackPanel = sender as StackPanel;
            // 确保 ListView 正确引用
            var listView = stackPanel?.FindName("FolderListView") as ListView;

            if (listView != null)
            {
                // Toggle visibility of ListView
                listView.Visibility = listView.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            }
        }




        private async void RenameFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem &&
                menuItem.DataContext is Folder folder)
            {
                // 创建对话框
                ContentDialog renameDialog = new ContentDialog
                {
                    Title = "重命名文件夹",
                    PrimaryButtonText = "确定",
                    SecondaryButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot // 非常重要：WinUI 3 中必须设置 XamlRoot
                };

                // 创建输入框
                TextBox inputTextBox = new TextBox
                {
                    Text = folder.Title,
                    PlaceholderText = "请输入新名称",
                    AcceptsReturn = false
                };

                // 添加输入框到对话框内容
                renameDialog.Content = inputTextBox;

                // 显示对话框并等待用户选择
                var result = await renameDialog.ShowAsync();

                // 用户点击“确定”
                if (result == ContentDialogResult.Primary)
                {
                    string newName = inputTextBox.Text.Trim();
                    if (!string.IsNullOrEmpty(newName))
                    {
                        folder.Title = newName;

                        _dataService.SaveToFile(folder);
                    }
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text;
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Search(keyword);
            }
        }


        private void FilterFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ApplyFilter("Favorite");
            }
        }

        private void FilterRecent_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ApplyFilter("Recent");
            }
        }

        private void FilterNote_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ApplyFilter("Note");
            }
        }

        private void FilterFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ApplyFilter("Folder");
            }
        }

        private void FilterClear_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ApplyFilter("None");
            }
        }
        private void IsImportant_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Folder folder)
            {
                if (folder != null)
                {
                    folder.IsFavorite = !folder.IsFavorite; // 切换收藏状态
                    _dataService.SaveToFile(folder);
                }
            }
            else if (sender is FrameworkElement feNote && feNote.DataContext is Note note)
            {
                if (note != null)
                {
                    note.IsFavorite = !note.IsFavorite; // 切换重要状态
                    _dataService.SaveToFile(note);
                }
            }
        }
        private void ToggleMultiSelect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ToggleMultiSelect();
                if (viewModel.IsMultiSelectEnabled)
                {
                    MyTreeView.SelectionMode = ListViewSelectionMode.Multiple;
                }
                else MyTreeView.SelectionMode = ListViewSelectionMode.Single;
            }
        }

        private void MyTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                foreach (var item in e.AddedItems)
                {
                    if (item is Calendar || item is Schedule)
                    {
                        MyTreeView.SelectedItems.Remove(item);
                        continue;
                    }
                    if (!vm.SelectedItems.Contains(item))
                        vm.SelectedItems.Add(item as Item);
                }

                foreach (var item in e.RemovedItems)
                {
                    vm.SelectedItems.Remove(item as Item);
                }
            }
        }


        private void OnFavoriteClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.FavoriteSelectedItems();
            }
        }

        private void UnFavoriteClicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.UnFavoriteSelectedItems();
            }

        }

        private void DeleteSelected_Clicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DeleteSelectedItems();
            }
        }

        private void MoveSelected_Clicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MoveSelectedItemsToFolder();
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var topLevelItems = vm.Items.Where(i => string.IsNullOrEmpty(i.beFolderID)).ToList();

                if (vm.IsAllSelected)
                {
                    // 取消全选
                    MyTreeView.SelectedItems.Clear();
                    vm.SelectedItems.Clear();
                    vm.IsAllSelected = false;
                }
                else
                {
                    // 执行全选
                    MyTreeView.SelectedItems.Clear();
                    foreach (var item in topLevelItems)
                    {
                        MyTreeView.SelectedItems.Add(item);
                    }

                    vm.SelectedItems.Clear();
                    foreach (var item in MyTreeView.SelectedItems)
                    {
                        vm.SelectedItems.Add(item as Item);
                    }

                    vm.IsAllSelected = true;
                }
            }
        }
    }

}
