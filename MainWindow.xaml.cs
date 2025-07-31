using IDEAs.Services;
using IDEAs.ViewModels;
using IDEAs.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;
using WinRT.Interop;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DataService _dataService;

        private AppWindow m_AppWindow;
        public bool isFullScreen = false;
        private ButtonHandler _buttonHandler;
        private WindowInitializer _windowInitializer;
        private bool isInitialized = false;
        public string BackgroundPath
        {
            get => _dataService._backgroundPath;
            set
            {
                if (_dataService._backgroundPath != value)
                {
                    _dataService._backgroundPath = value;
                }
            }
        }
        public double BackgroundOpacity
        {
            get => _dataService.BackgroundOpacity;
            set
            {
                if (_dataService.BackgroundOpacity != value)
                {
                    _dataService.BackgroundOpacity = value;
                }
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            MainFrame.Navigate(typeof(MainPage));
            _dataService.PropertyChanged += DataService_PropertyChanged;

            // 扩展内容到标题栏区域
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            // 获取页面中的 ColumnDefinition 元素
            MainPage mainPage = MainFrame.Content as MainPage;

            m_AppWindow = GetAppWindowForCurrentWindow();

            // 初始化窗口设置
            _windowInitializer = new WindowInitializer(this, AppTitleBar, LeftPaddingColumn, RightPaddingColumn, SettingsButton, FullScreenButton, BackButton);


            // 初始化按钮处理器
            _buttonHandler = new ButtonHandler(m_AppWindow, MainFrame, this, FullScreenButton, mainPage.FindName("LeftColumn") as ColumnDefinition, mainPage.FindName("RightColumn") as ColumnDefinition, mainPage.FindName("ContentFrame") as Frame);
            this.Closed += MainWindow_Closed;
            this.Activated += MainWindow_Activated;
        }
        private void DataService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataService._backgroundPath))
            {
                // 当 DataService 的 BackgroundPath 发生变化时，触发 MainViewModel 的 PropertyChanged
                OnPropertyChanged(nameof(BackgroundPath));
            }
            else if (e.PropertyName == nameof(BackgroundOpacity))
                OnPropertyChanged(nameof(BackgroundOpacity));
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            var appWindow = GetAppWindowForCurrentWindow();
            var width = appWindow.Size.Width;
            var height = appWindow.Size.Height;
            var posX = appWindow.Position.X;
            var posY = appWindow.Position.Y;

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["WindowWidth"] = width;
            localSettings.Values["WindowHeight"] = height;
            localSettings.Values["WindowPosX"] = posX;
            localSettings.Values["WindowPosY"] = posY;
        }

        private void MainWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            if (!isInitialized)
            {
                RestoreWindowSizeAndPosition();
                isInitialized = true;
            }
        }
        private void RestoreWindowSizeAndPosition()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("WindowWidth") && localSettings.Values.ContainsKey("WindowHeight"))
            {
                int width = (int)localSettings.Values["WindowWidth"];
                int height = (int)localSettings.Values["WindowHeight"];
                int posX = (int)localSettings.Values["WindowPosX"];
                int posY = (int)localSettings.Values["WindowPosY"];

                var appWindow = GetAppWindowForCurrentWindow();
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(posX, posY, width, height));
            }
        }

        // 按钮处理程序
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _buttonHandler.SettingsButton_Click(sender, e);
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            _buttonHandler.FullScreenButton_Click(sender, e);
            isFullScreen = !isFullScreen;
            if (isFullScreen)
            {
                HideTitleBar();
            }
            // 重新设置标题栏区域
            _windowInitializer.SetRegionsForCustomTitleBar();

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _buttonHandler.BackButton_Click(sender, e);

        }
        public void ShowTitleBar()
        {
            AppTitleBar.Visibility = Visibility.Visible;
        }

        public void HideTitleBar()
        {
            AppTitleBar.Visibility = Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }
}
