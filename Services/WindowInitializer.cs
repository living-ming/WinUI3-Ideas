using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace IDEAs.Services
{
    internal class WindowInitializer
    {
        private AppWindow _appWindow;
        private Grid _appTitleBar;
        private ColumnDefinition _leftPaddingColumn;
        private ColumnDefinition _rightPaddingColumn;
        private Button _settingsButton;
        private Button _fullScreenButton;
        private Button _backButton;

        public WindowInitializer(Window window, Grid appTitleBar,
                                 ColumnDefinition leftPaddingColumn, ColumnDefinition rightPaddingColumn,
                                 Button settingsButton, Button fullScreenButton, Button backButton)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appTitleBar = appTitleBar;
            _leftPaddingColumn = leftPaddingColumn;
            _rightPaddingColumn = rightPaddingColumn;
            _settingsButton = settingsButton;
            _fullScreenButton = fullScreenButton;
            _backButton = backButton;

            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(appTitleBar);

            _appTitleBar.Loaded += AppTitleBar_Loaded;
            _appTitleBar.SizeChanged += AppTitleBar_SizeChanged;

        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            SetRegionsForCustomTitleBar();
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetRegionsForCustomTitleBar();
        }

        public void SetRegionsForCustomTitleBar()
        {
            double scaleAdjustment = _appTitleBar.XamlRoot.RasterizationScale;

            // 设置左右填充列的宽度
            _rightPaddingColumn.Width = new GridLength(_appWindow.TitleBar.RightInset / scaleAdjustment);
            _leftPaddingColumn.Width = new GridLength(_appWindow.TitleBar.LeftInset / scaleAdjustment);

            // 获取设置按钮、全屏按钮和后退按钮的边界
            Windows.Graphics.RectInt32 settingsButtonRect = GetRect(_settingsButton.TransformToVisual(null).TransformBounds(new Rect(0, 0, _settingsButton.ActualWidth, _settingsButton.ActualHeight)), scaleAdjustment);
            Windows.Graphics.RectInt32 fullScreenButtonRect = GetRect(_fullScreenButton.TransformToVisual(null).TransformBounds(new Rect(0, 0, _fullScreenButton.ActualWidth, _fullScreenButton.ActualHeight)), scaleAdjustment);
            Windows.Graphics.RectInt32 backButtonRect = GetRect(_backButton.TransformToVisual(null).TransformBounds(new Rect(0, 0, _backButton.ActualWidth, _backButton.ActualHeight)), scaleAdjustment);

            // 设置非客户端区域
            var rectArray = new Windows.Graphics.RectInt32[] { settingsButtonRect, fullScreenButtonRect, backButtonRect };
            InputNonClientPointerSource nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);
            nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
        }

        private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        {
            return new Windows.Graphics.RectInt32(
                _X: (int)Math.Round(bounds.X * scale),
                _Y: (int)Math.Round(bounds.Y * scale),
                _Width: (int)Math.Round(bounds.Width * scale),
                _Height: (int)Math.Round(bounds.Height * scale)
            );
        }

    }
}
