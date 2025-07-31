using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace IDEAs.Models
{
    public class Folder : Item, INotifyPropertyChanged
    {
        // 使用自动初始化的属性，确保每次创建 Folder 时 _Item 都是一个有效的 ObservableCollection
        [JsonIgnore]
        public ObservableCollection<Item> Items1 { get; set; } = new ObservableCollection<Item>();

        // 唯一的文件夹 ID
        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public Folder() : base()
        {
            FileType = "Folder";
        }
    }
}

