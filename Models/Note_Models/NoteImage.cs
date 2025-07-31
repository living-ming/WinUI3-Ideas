using System.ComponentModel;

namespace IDEAs.Models.Note_Models
{
    public class NoteImage : INotifyPropertyChanged
    {
        public string ImagePath { get; set; }

        // 当前实际容器宽高（用于计算真实坐标）
        public double ContainerWidth { get; set; }
        public double ContainerHeight { get; set; }

        // 相对位置（0~1）
        public double RelativeX { get; set; }
        public double RelativeY { get; set; }

        // 绑定用的真实位置（用于 UI 展示）
        public double X => RelativeX * ContainerWidth;
        public double Y => RelativeY * ContainerHeight;

        public double _Width { get; set; }
        public double _Height { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPositionChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y)));
        }

        public void UpdateFromAbsolutePosition(double x, double y)
        {
            if (ContainerWidth > 0 && ContainerHeight > 0)
            {
                RelativeX = x / ContainerWidth;
                RelativeY = y / ContainerHeight;
                NotifyPositionChanged();
            }
        }

        public void UpdateContainerSize(double width, double height)
        {
            ContainerWidth = width;
            ContainerHeight = height;
            NotifyPositionChanged();
        }
    }


}
