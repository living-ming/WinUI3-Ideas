using IDEAs.Models;
using IDEAs.Models.Note_Models;
using IDEAs.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IDEAs.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotePage : Page, INotifyPropertyChanged
    {
        private Note _note;
        private bool isResizing = false;
        private Point initialPoint;
        private double initialWidth, initialHeight;
        private DataService _dataService;
        private double imageContainerWidth = 0;
        private double imageContainerHeight = 0;
        private Color _currentColor = Colors.Black;
        private string _noteInfoDisplay;
        private bool temp = false;
        public string NoteInfoDisplay
        {
            get => _noteInfoDisplay;
            set
            {
                _noteInfoDisplay = value;
                OnPropertyChanged(nameof(NoteInfoDisplay)); // 若无通知机制，可直接手动设置
            }
        }
        public bool ShowWordCount
        {
            get => _dataService.ShowWordCount;
        }
        public bool HighlightComments
        {
            get => _dataService.HighlightComments;
        }

        public NotePage()
        {
            this.InitializeComponent();
            _dataService = ((App)Application.Current).DataService;
            Loaded += Page_Loaded;
        }


        // 1. 页面加载时恢复列宽
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (settings.TryGetValue("LeftWidthRatio", out var raw) && raw is double ratio)
            {
                // 第一列占 ratio，第三列占 1 - ratio
                NoteGrid.ColumnDefinitions[0].Width =
                    new GridLength(ratio, GridUnitType.Star);
                NoteGrid.ColumnDefinitions[2].Width =
                    new GridLength(1 - ratio, GridUnitType.Star);
            }
            if (settings.TryGetValue("TopRowRatio", out raw) && raw is double topRatio)
            {
                // 第一行占 topRatio，第二行占 1 - topRatio
                NoteGrid.RowDefinitions[0].Height =
                    new GridLength(topRatio, GridUnitType.Star);
                NoteGrid.RowDefinitions[2].Height =
                    new GridLength(1 - topRatio, GridUnitType.Star);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _dataService = ((App)Application.Current).DataService;
            if (e.Parameter is Note note)
            {
                _note = note;

                if (_note.Images == null)
                    _note.Images = new ObservableCollection<NoteImage>();

                this.DataContext = _note;

                if (!string.IsNullOrEmpty(_note.Content))
                    ContentBox.Document.SetText(TextSetOptions.FormatRtf, _note.Content);
                else
                    ContentBox.Document.SetText(TextSetOptions.FormatRtf, "");
            }

            ColorGrid.ItemsSource = new List<ColorItem>
{
    new() { Name = "Black", Color = new SolidColorBrush(Colors.Black) },
    new() { Name = "White", Color = new SolidColorBrush(Colors.White) },
    new() { Name = "Red", Color = new SolidColorBrush(Colors.Red) },
    new() { Name = "Orange", Color = new SolidColorBrush(Colors.Orange) },
    new() { Name = "Yellow", Color = new SolidColorBrush(Colors.Yellow) },
    new() { Name = "Green", Color = new SolidColorBrush(Colors.Green) },
    new() { Name = "Blue", Color = new SolidColorBrush(Colors.Blue) },
    new() { Name = "Purple", Color = new SolidColorBrush(Colors.Purple) },
    new() { Name = "Pink", Color = new SolidColorBrush(Colors.Pink) },
    new() { Name = "Brown", Color = new SolidColorBrush(Colors.Brown) },
    new() { Name = "Teal", Color = new SolidColorBrush(Colors.Teal) },
};
        }

        public void SavaData()
        {
            if (((App)Application.Current).DeletedNoteNames.Contains(_note?.Name))
            {
                Debug.WriteLine("🟡 当前Note已在 MainPage 中被删除，跳过保存。");
                return;
            }

            ContentBox.Document.GetText(TextGetOptions.FormatRtf, out string rtfContent);
            _note.Content = rtfContent;
            ContentBox.Document.GetText(TextGetOptions.None, out string plainText);
            _note.PlainTextContent = plainText;
            _dataService.SaveToFile(_note, temp);
        }

        private void OnAddAnnotationClick(object sender, RoutedEventArgs e)
        {
            var selection = ContentBox.Document.Selection;

            // 获取选中的文字
            selection.GetText(TextGetOptions.None, out string selectedText);
            selectedText = selectedText?.Trim();

            if (string.IsNullOrEmpty(selectedText) || selection.StartPosition == selection.EndPosition)
                return;

            var start = selection.StartPosition;
            var end = selection.EndPosition;
            if (HighlightComments)
            {
                // 应用样式给选中的部分
                var format = selection.CharacterFormat;
                format.Underline = UnderlineType.Single;
                format.ForegroundColor = Colors.Blue;

                // ⛔ 解决关键：重置插入点样式，防止格式“泄漏”到新输入
                ContentBox.Document.Selection.SetRange(end, end); // 移动光标到选区末尾
                var resetFormat = ContentBox.Document.Selection.CharacterFormat;
                resetFormat.Underline = UnderlineType.None;
                resetFormat.ForegroundColor = Colors.Black;
            }
            // 添加注释对象
            var anno = new Annotation
            {
                Fragment = selectedText,
                StartIndex = start,
                EndIndex = end,
                Comment = string.Empty
            };

            _note.Annotations.Add(anno);
        }
        private void FontDefautClick(object sender, RoutedEventArgs e)
        {
            var selection = ContentBox.Document.Selection;
            // 获取选中的文字
            selection.GetText(TextGetOptions.None, out string selectedText);
            selectedText = selectedText?.Trim();
            var format = selection.CharacterFormat;
            format.Underline = UnderlineType.None;
            format.ForegroundColor = Colors.Black;
            format.Bold = FormatEffect.Off;
            format.Italic = FormatEffect.Off;
        }
        private void OnDeleteAnnotation(object sender, RoutedEventArgs e)
        {
            // 从按钮的 DataContext 拿到对应的 Annotation 实例
            if ((sender as Button)?.DataContext is Annotation anno)
                _note.Annotations.Remove(anno);
        }

        private void MainScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // 监听 Preview 级别的 PointerWheel 事件（而不是 PointerWheelChanged）
            MainScrollViewer.AddHandler(UIElement.PointerWheelChangedEvent,
                new PointerEventHandler(ScrollViewer_PointerWheelChanged), true);
        }

        private void ScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                var delta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;

                // 水平方向偏移
                double newOffset = scrollViewer.HorizontalOffset - delta;

                scrollViewer.ChangeView(newOffset, null, null);
                e.Handled = true;
            }
        }

        private void ImageContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            imageContainerWidth = e.NewSize.Width;
            imageContainerHeight = e.NewSize.Height;

            // 更新所有图片的容器尺寸
            foreach (var image in _note.Images)
            {
                image.UpdateContainerSize(imageContainerWidth, imageContainerHeight);
            }
        }


        // 当用户按下图片时，判断是否是在边缘
        private void OnImagePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var image = sender as Image;
            var position = e.GetCurrentPoint(image).Position;
            var tolerance = 30; // 边缘的检测容差，比如 10 个像素
            initialPoint = position;

            // 获取图片的当前尺寸
            initialWidth = image.ActualWidth;
            initialHeight = image.ActualHeight;

            // 检查是否在图片边缘
            if (position.X >= initialWidth - tolerance || position.Y >= initialHeight - tolerance)
            {
                // 如果在边缘，准备开始缩放
                isResizing = true;
                image.CapturePointer(e.Pointer); // 捕获鼠标指针
            }
        }

        // 当用户拖动时，如果正在缩放，调整图片尺寸
        private void OnImagePointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing)
            {
                var image = sender as Image;
                var position = e.GetCurrentPoint(image).Position;

                // 计算新的尺寸
                double deltaX = position.X - initialPoint.X;
                double deltaY = position.Y - initialPoint.Y;

                // 通过 delta 调整图片大小
                double newWidth = initialWidth + deltaX;
                double newHeight = initialHeight + deltaY;

                // 确保图片尺寸不小于最小值
                if (newWidth > 50 && newHeight > 50)
                {
                    image.Width = newWidth;
                    image.Height = newHeight;

                    var noteImage = (NoteImage)((FrameworkElement)sender).DataContext;
                    noteImage._Width = newWidth;
                    noteImage._Height = newHeight;
                }
            }
        }

        // 当用户松开鼠标时，结束缩放操作
        private void OnImagePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                var image = sender as Image;
                image.ReleasePointerCaptures();
                SavaData();

            }
        }
        private void OnImageManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var element = sender as UIElement;
            var transform = element?.RenderTransform as CompositeTransform;
            if (transform == null) return;

            if (!isResizing)
            {
                // 拖动更新位置
                transform.TranslateX += e.Delta.Translation.X;
                transform.TranslateY += e.Delta.Translation.Y;
            }

            var noteImage = (NoteImage)((FrameworkElement)sender).DataContext;

            // 反向计算相对坐标
            double absX = transform.TranslateX;
            double absY = transform.TranslateY;
            noteImage.UpdateFromAbsolutePosition(absX, absY);
        }
        private void OnDeleteImageClick(object sender, RoutedEventArgs e)
        {
            // MenuFlyoutItem.DataContext 会自动是对应的 NoteImage
            if ((sender as MenuFlyoutItem)?.DataContext is NoteImage img)
            {
                _note.Images.Remove(img);
            }
        }
        private void SavaClick(object sender, RoutedEventArgs e)
        {

            SavaData(); // 你的原方法
        }


        private void OnIncreaseFontSize(object sender, RoutedEventArgs e)
        {
            ChangeSelectedTextFontSize(2);
        }

        private void OnDecreaseFontSize(object sender, RoutedEventArgs e)
        {
            ChangeSelectedTextFontSize(-2); // 减少2号字体
        }
        private void ChangeSelectedTextFontSize(float delta)
        {
            var selection = ContentBox.Document.Selection;

            // 如果没有选中内容，则设置插入点的默认字体大小
            if (selection.Length == 0)
            {
                selection.CharacterFormat.Size += delta;
            }
            else
            {
                // 获取当前选中文本的字体大小
                float currentSize = selection.CharacterFormat.Size;
                float newSize = Math.Max(1, currentSize + delta); // 不小于1

                selection.CharacterFormat.Size = newSize;
            }
        }

        private void OnColorBlockClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorName)
            {
                var selection = ContentBox.Document.Selection;

                // 解析颜色名为 Color
                var colorProp = typeof(Colors).GetProperty(colorName);
                var color = colorProp;

                if (colorProp != null)
                {
                    _currentColor = (Color)colorProp.GetValue(null);

                    // 更新 SplitButton 的颜色指示器
                    CurrentColorIndicator.Background = new SolidColorBrush(_currentColor);
                }
                if (string.IsNullOrEmpty(selection.CharacterFormat.Name))
                {
                    selection.CharacterFormat.Name = "Segoe UI";
                }

                selection.CharacterFormat.ForegroundColor = (Color)colorProp.GetValue(null);
                ColorFlyout.Hide();
            }
        }
        private void ColorSplitButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            // 在这里将颜色应用到目标，例如：
            var selection = ContentBox?.Document?.Selection;
            if (selection != null)
            {
                if (string.IsNullOrEmpty(selection.CharacterFormat.Name))
                {
                    selection.CharacterFormat.Name = "Segoe UI";
                }

                selection.CharacterFormat.ForegroundColor = _currentColor;
            }
        }


        private void OnUndoClick(object sender, RoutedEventArgs e)
        {
            ContentBox.Document.Undo();
        }
        private void OnRedoClick(object sender, RoutedEventArgs e)
        {
            ContentBox.Document.Redo();
        }

        private async void OnSelectImagesClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");

            // 获取主窗口句柄
            var hwnd = WindowNative.GetWindowHandle(App.m_window); // 使用 App.MainWindow 获取句柄

            // 将窗口句柄与 FolderPicker 关联
            InitializeWithWindow.Initialize(picker, hwnd);


            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (var file in files)
                {
                    _note.Images.Add(new NoteImage
                    {
                        ImagePath = file.Path,
                        RelativeX = 0.0,
                        RelativeY = 0.0,
                        ContainerWidth = imageContainerWidth,   // 需要传入当前容器宽高
                        ContainerHeight = imageContainerHeight,
                        _Width = 200,
                        _Height = 200
                    });

                }
            }
        }
        private void OnSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // 总宽度 = 整个 Grid 宽度 - 分隔条宽度
            double total = NoteGrid.ActualWidth - MySplitter.ActualWidth;
            double left = NoteGrid.ColumnDefinitions[0].ActualWidth;
            double ratio = left / total;

            ApplicationData.Current.LocalSettings
                .Values["LeftWidthRatio"] = ratio;
        }
        private void OnRowSplitter_ManipulationCompleted(
    object sender,
    ManipulationCompletedRoutedEventArgs e)
        {
            // 可用总高度 = Grid 总高度 - 拆分条高度
            double total = NoteGrid.ActualHeight - RowSplitter.ActualHeight;
            double top = TopRow.ActualHeight;
            double ratio = top / total;

            ApplicationData.Current
                .LocalSettings
                .Values["TopRowRatio"] = ratio;
        }


        private void FontSizePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizePicker.SelectedItem is ComboBoxItem item &&
                float.TryParse(item.Content.ToString(), out float selectedSize))
            {
                var selection = ContentBox.Document.Selection;
                if (selection.Length == 0)
                {
                    selection.CharacterFormat.Size = selectedSize;
                }
                else
                if (!string.IsNullOrEmpty(selection.Text))
                {
                    selection.CharacterFormat.Size = selectedSize;

                }

            }
        }
        private void FontSizePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.IsDropDownOpen = true;
            }
        }

        private void MyRichEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                // 插入制表符
                var richEditBox = sender as RichEditBox;
                richEditBox.Document.Selection.TypeText("\t");

                // 阻止事件继续传播，防止焦点移动
                e.Handled = true;
            }

        }


        // 解决某些情况下 ComboBox 未能展开的问题
        private void FontSizeFlyout_Opened(object sender, object e)
        {
            FontSizePicker.IsDropDownOpen = true;
        }
        private void OnCtrlSInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            SavaData(); // 或 SaveData

        }

        private void OnCtrlBInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            ContentBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
        }

        private void OnCtrlIInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            ContentBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
        }

        private void richEditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            UpdateNoteInfoDisplay();
            SavaData();     // 文本内容改变时自动保存
            temp = true;
        }
        private void UpdateNoteInfoDisplay()
        {
            int wordCount = GetWordCountFromRichEditBox(ContentBox);
            string time = _note.LastAccessed.ToString("yyyy-MM-dd HH:mm");
            NoteInfoDisplay = $"字数：{wordCount}    修改时间：{time}";
        }
        private int GetWordCountFromRichEditBox(RichEditBox box)
        {
            box.Document.GetText(TextGetOptions.UseObjectText, out string text);
            return string.IsNullOrWhiteSpace(text) ? 0 : text.Trim().Length;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnCtrlPInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ChangeSelectedTextFontSize(2);
        }

        private void OnCtrlOInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            ChangeSelectedTextFontSize(-2);
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}