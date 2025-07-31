using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace IDEAs.Models.Note_Models
{

    public class Annotation : INotifyPropertyChanged
    {
        // 屏幕上让用户编辑的注释内容
        private string _comment;
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }

        // 选中文本的起始／结束索引
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }

        // 被注释的原始文本片段（只读，初始化时赋值）
        public string Fragment { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
