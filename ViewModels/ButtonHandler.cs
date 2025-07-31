using IDEAs.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace IDEAs.ViewModels
{
    internal class ButtonHandler
    {
        private AppWindow _appWindow;
        private Frame _mainFrame;
        private MainWindow _mainWindow;
        private Button _fullScreenButton;
        private ColumnDefinition _leftColumn;
        private ColumnDefinition _rightColumn;
        public bool _isFullScreen = false;
        private Frame _contentFrame;

        public ButtonHandler(AppWindow appWindow, Frame mainFrame, MainWindow mainWindow, Button fullScreenButton, ColumnDefinition leftColumn, ColumnDefinition rightColumn, Frame contentFrame)
        {
            _appWindow = appWindow;
            _mainFrame = mainFrame;
            _mainWindow = mainWindow;
            _fullScreenButton = fullScreenButton;
            _leftColumn = leftColumn;
            _rightColumn = rightColumn;
            _contentFrame = contentFrame;
            _leftColumn.MinWidth = 0;
        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_leftColumn.Width.Value != 0)
            {
                _leftColumn.Width = new GridLength(0, GridUnitType.Star);
            }
            else if (_leftColumn.Width.Value == 0)
            {
                // 展开左侧区域
                _leftColumn.Width = new GridLength(0.42, GridUnitType.Star);
            }

        }

        public void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查 _mainFrame 是否不为空
            if (_mainFrame != null)
            {
                // 检查当前页面是否已经是 SettingsPage
                if (_contentFrame.Content is SettingsPage)
                {
                    //返回上级页面的方法
                    _contentFrame.Navigate(typeof(CalendarPage));
                }
                else
                {
                    // 导航到 SettingsPage 页面
                    _contentFrame.Navigate(typeof(SettingsPage));
                    Debug.WriteLine("Navigating to SettingsPage"); // 输出调试信息：正在导航到 SettingsPage
                }
            }
        }

        public void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen)
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            }
            else
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
            _isFullScreen = !_isFullScreen;
        }

    }
}
