using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace IDEAs.Models
{
    public abstract class Item : ObservableObject
    {
        public string Name { get; set; }
        public string beFolderID { get; set; }
        public string FileType { get; set; }
        public DateTime LastAccessed { get; set; }
        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged(nameof(IsFavorite));
                    OnPropertyChanged(nameof(IsImportantGlyph));
                }
            }
        }

        public string IsImportantGlyph => IsFavorite ? "\uE735" : "\uE734";

        public Item() { } // 无参数构造函数
    }
}
