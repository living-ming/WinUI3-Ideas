using System;
using System.ComponentModel;

namespace IDEAs.Models
{
    public class Schedule_Event : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DateTimeOffset? _startTime;
        public DateTimeOffset? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(StartTime));
                }
            }
        }

        private DateTimeOffset? _endTime;
        public DateTimeOffset? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(EndTime));
                }
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(Title));
                }
            }
        }


        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        private bool _isImportant;
        public bool IsImportant
        {
            get => _isImportant;
            set
            {
                if (_isImportant != value)
                {
                    _isImportant = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(IsImportant));
                    OnPropertyChanged(nameof(IsImportantGlyph));
                }
            }
        }
        public string IsImportantGlyph => IsImportant ? "\uE735" : "\uE734";

        private string _category;
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    UpdateLastModifiedTime();
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        private DateTimeOffset _lastModifiedTime;
        public DateTimeOffset LastModifiedTime
        {
            get => _lastModifiedTime;
            set
            {
                if (_lastModifiedTime != value)
                {
                    _lastModifiedTime = value;
                }
            }
        }
        private void UpdateLastModifiedTime()
        {
            _lastModifiedTime = DateTimeOffset.Now;
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Schedule_Event()
        {
            // 初始化属性为默认值
            StartTime = DateTimeOffset.Now;
            EndTime = DateTimeOffset.Now.AddHours(1);
            Title = string.Empty;
            IsCompleted = false;
            IsImportant = false;
            Category = null;
            LastModifiedTime = DateTimeOffset.Now;
        }
    }
}
