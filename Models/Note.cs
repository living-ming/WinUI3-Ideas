using IDEAs.Models.Note_Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IDEAs.Models
{
    public class Note : Item, INotifyPropertyChanged
    {
        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }
        public bool IsFolder { get; set; } = false;
        // 保存文件路径
        public string FilePath { get; set; }

        // 注释列表
        private ObservableCollection<Annotation> _annotations;
        public ObservableCollection<Annotation> Annotations
        {
            get => _annotations;
            set
            {
                if (_annotations != value)
                {
                    _annotations = value;
                    OnPropertyChanged(nameof(Annotations));
                }
            }
        }

        private ObservableCollection<NoteImage> _images = new ObservableCollection<NoteImage>();
        public ObservableCollection<NoteImage> Images
        {
            get => _images;
            set
            {
                if (_images != value)
                {
                    _images = value;
                    OnPropertyChanged(nameof(Images));
                }
            }
        }

        public string PlainTextContent { get; set; }  // 提取出的纯文本内容

        // 构造函数
        public Note()
        {
            _annotations = new ObservableCollection<Annotation>();
            FileType = "Note";
        }

    }
}
