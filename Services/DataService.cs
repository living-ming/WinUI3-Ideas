using IDEAs.Models;
using IDEAs.Models.Note_Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace IDEAs.Services
{
    public class DataService : INotifyPropertyChanged
    {
        public string _dataFilePath;
        public string _backgroundPath;
        private double _backgroundOpacity = 0.5;
        private bool _showWordCount = true;
        private bool _highlightComments = true;
        private const string PathKey = "CustomSavePath";
        private const string BackgroundKey = "CustomBackgroundPath";
        private const string BackgroundOpacityKey = "CustomBackgroundOpacity";
        private const string WordCountKey = "WordCount";
        private const string HighlightCommentKey = "HighlightComment";
        public double BackgroundOpacity
        {
            get => _backgroundOpacity;
            set
            {
                if (_backgroundOpacity != value)
                {
                    _backgroundOpacity = value;
                    SaveCustomBackgroundOpacity(value);
                    OnPropertyChanged(nameof(BackgroundOpacity));
                }
            }
        }
        public bool ShowWordCount
        {
            get => _showWordCount;
            set
            {
                if (_showWordCount != value)
                {
                    _showWordCount = value;
                    SaveWordCount(value);
                    OnPropertyChanged(nameof(ShowWordCount));
                }
            }

        }
        public bool HighlightComments
        {
            get => _highlightComments;
            set
            {
                if (_highlightComments != value)
                {
                    _highlightComments = value;
                    SaveHighlightComment(value);
                    OnPropertyChanged(nameof(HighlightComments));
                }
            }

        }

        public DataService()
        {
            LoadCustomSavePath();
            LoadCustomBackgroundPath();
            LoadSwitch();
        }

        private void LoadCustomSavePath()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey(PathKey))
            {
                _dataFilePath = localSettings.Values[PathKey] as string;
            }
            else
            {
                // 设置默认保存路径：位于应用的 LocalFolder 下的 "files" 子目录
                var defaultFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "files");

                if (!Directory.Exists(defaultFolder))
                {
                    Directory.CreateDirectory(defaultFolder);
                }

                _dataFilePath = defaultFolder;

                // 可选：首次写入默认路径到设置中，便于统一管理
                localSettings.Values[PathKey] = _dataFilePath;
            }
        }

        private void LoadCustomBackgroundPath()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey(BackgroundKey))
                _backgroundPath = localSettings.Values[BackgroundKey] as string;
            else
            {
                _backgroundPath = "F:\\_Ideass\\IDEAs\\Assets\\FFFFFF.png";  // 如果没有设置背景路径，则为空，表示不使用背景图
            }

            if (localSettings.Values.ContainsKey(BackgroundOpacityKey))
                _backgroundOpacity = Convert.ToDouble(localSettings.Values[BackgroundOpacityKey]);
        }
        private void LoadSwitch()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(WordCountKey))
            {
                _showWordCount = Convert.ToBoolean(localSettings.Values[WordCountKey]);
            }
            if (localSettings.Values.ContainsKey(HighlightCommentKey))
            {
                _highlightComments = Convert.ToBoolean(localSettings.Values[HighlightCommentKey]);
            }
        }
        public async Task SetCustomBackgroundPathAsync()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".webp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                _backgroundPath = file.Path;
                OnPropertyChanged(nameof(_backgroundPath));
                SaveCustomBackgroundPath(_backgroundPath);
            }
        }
        private void SaveWordCount(bool temp)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[WordCountKey] = temp;
        }
        private void SaveHighlightComment(bool te)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[HighlightCommentKey] = te;

        }
        private void SaveCustomBackgroundPath(string path)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[BackgroundKey] = path;
        }
        private void SaveCustomBackgroundOpacity(double opacity)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[BackgroundOpacityKey] = opacity;
        }

        public async Task SetCustomSavePathAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string oldPath = _dataFilePath;
                string newPath = folder.Path;

                // 如果新旧路径不同且旧路径存在，则迁移数据
                if (!string.IsNullOrEmpty(oldPath) && oldPath != newPath && Directory.Exists(oldPath))
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(oldPath))
                        {
                            string fileName = Path.GetFileName(file);
                            string destPath = Path.Combine(newPath, fileName);

                            // 移动文件（如需保留旧文件可改为 File.Copy）
                            using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                            using (FileStream destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                            {
                                sourceStream.CopyTo(destStream);
                            }

                            // 删除原文件
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: 异常处理
                        Debug.WriteLine($"迁移文件失败：{ex.Message}");
                    }
                }

                // 更新保存路径
                _dataFilePath = newPath;
                SaveCustomSavePath(_dataFilePath);
            }
        }

        private void SaveCustomSavePath(string path)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[PathKey] = path;
        }


        public ObservableCollection<Item> LoadData()
        {
            var items = new List<Item>();
            var folderDict = new Dictionary<string, Folder>();
            var files = Directory.GetFiles(_dataFilePath, "*.json");

            // 预处理文件
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var root = JsonDocument.Parse(json).RootElement;

                if (!root.TryGetProperty("FileType", out JsonElement fileTypeElement))
                {
                    // 兼容旧数据，默认当作 Folder
                    var folder = JsonSerializer.Deserialize<Folder>(json, AppJsonContext.Default.Folder);
                    if (folder != null)
                    {
                        items.Add(folder);
                        folderDict[folder.Name] = folder;
                    }
                    continue;
                }

                var fileType = fileTypeElement.GetString()?.Trim();
                switch (fileType)
                {
                    case "Note":
                        var note = JsonSerializer.Deserialize<Note>(json, AppJsonContext.Default.Note);
                        if (note != null)
                            items.Add(note);
                        break;

                    case "Folder":
                        var folder = JsonSerializer.Deserialize<Folder>(json, AppJsonContext.Default.Folder);
                        if (folder != null)
                        {
                            items.Add(folder);
                            folderDict[folder.Name] = folder;
                        }
                        break;

                    default:
                        break;
                }
            }
            // 处理具有beFolderID的项
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.beFolderID) && folderDict.TryGetValue(item.beFolderID, out var parentFolder))
                {
                    parentFolder.Items1.Add(item);
                }
            }

            // 过滤掉具有beFolderID的项（顶层文件夹）
            items = items.Where(i => string.IsNullOrEmpty(i.beFolderID)).ToList();

            // 根据特别收藏和最近使用排序
            items = items
                .OrderByDescending(f => f.IsFavorite)
                .ThenByDescending(f => f.LastAccessed)
                .ToList();

            return new ObservableCollection<Item>(items);
        }

        public void SaveToFile<T>(T item, bool temp = true) where T : Item
        {
            if (temp) item.LastAccessed = DateTime.Now;
            string filePath = Path.Combine(_dataFilePath, $"{item.Name}.json");
            string json = item switch
            {
                Note note => JsonSerializer.Serialize(note, AppJsonContext.Default.Note),
                Folder folder => JsonSerializer.Serialize(folder, AppJsonContext.Default.Folder),
                Schedule schedule => JsonSerializer.Serialize(schedule, AppJsonContext.Default.Schedule),
                _ => throw new NotSupportedException("Unsupported item type")
            };
            File.WriteAllText(filePath, json);
        }
        public async Task<Schedule> LoadSchedule()
        {
            var scheduleFile = Path.Combine(_dataFilePath, $"Schedule.json");
            if (File.Exists(scheduleFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(scheduleFile);
                    var schedule = JsonSerializer.Deserialize(json, AppJsonContext.Default.Schedule);

                    // 确保内部集合不为 null
                    if (schedule.Schedules == null)
                    {
                        schedule.Schedules = new ObservableCollection<Schedule_Event>();
                    }
                    if (schedule.Categories == null)
                    {
                        schedule.Categories = new ObservableCollection<string>();
                    }

                    return schedule;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载日程表时发生错误：{ex.Message}");
                    return null;
                }
            }

            return null;
        }

        public Folder CreateFolder()
        {
            var folder = new Folder
            {
                Name = Guid.NewGuid().ToString("N"), // 永久唯一标识
                Title = "New Folder",
            };
            SaveToFile(folder);
            return folder;
        }

        public Note CreateNote()
        {
            var note = new Note
            {
                Name = Guid.NewGuid().ToString("N"), // 永久唯一标识
                Title = "New Note",
                Images = new ObservableCollection<NoteImage>()
            };
            SaveToFile(note);
            return note;
        }
        public string GetFilePath()
        {
            return _dataFilePath;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public class ItemConverter : JsonConverter<Item>
    {
        public override Item Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("FileType", out JsonElement fileTypeElement))
                {
                    string type = fileTypeElement.GetString();
                    switch (type)
                    {
                        case "Note":
                            return JsonSerializer.Deserialize(root.GetRawText(), AppJsonContext.Default.Note);
                        case "Folder":
                            return JsonSerializer.Deserialize(root.GetRawText(), AppJsonContext.Default.Folder);
                        case "Schedule":
                            return JsonSerializer.Deserialize(root.GetRawText(), AppJsonContext.Default.Schedule);
                        default:
                            return null;
                    }
                }
                else
                {
                    return JsonSerializer.Deserialize(root.GetRawText(), AppJsonContext.Default.Folder);
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
        {
            if (value is Note note)
            {
                JsonSerializer.Serialize(writer, note, AppJsonContext.Default.Note);
            }
            else if (value is Folder folder)
            {
                JsonSerializer.Serialize(writer, folder, AppJsonContext.Default.Folder);
            }
            else if (value is Schedule schedule)
            {
                JsonSerializer.Serialize(writer, schedule, AppJsonContext.Default.Schedule);
            }
            else
            {
                throw new NotSupportedException($"Unknown Item type: {value?.GetType().Name}");
            }
        }
    }
}


