using IDEAs.Services;
using Microsoft.UI.Xaml;
using System.Collections.Generic;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public DataService DataService { get; private set; }//设置一个全局dataservice，使设置页面的保存路径修改时，主页面创建的文件路径正确
        public HashSet<string> DeletedNoteNames { get; } = new HashSet<string>(); // 用于共享删除记录

        public App()
        {

            this.InitializeComponent();
            DataService = new DataService();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        public static Window m_window { get; private set; }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

        }


    }
}
