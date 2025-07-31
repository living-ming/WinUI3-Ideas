using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IDEAs.Models;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;


namespace IDEAs.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DataService _dataService;
        public Calendar Calendar = new();
        public Schedule Schedule = new();
        public Item _selectedItem;
        public Folder _selectedFolder;
        public ObservableCollection<Item> Items = new();
        public Func<List<Folder>, Task<Folder>> FolderSelectionDialog { get; set; }
        public ICommand CreateFolderCommand { get; }
        public ICommand CreateNoteCommand { get; }
        public ICommand ShowAllFoldersCommand { get; }
        private List<Item> _allItemsFlat = new(); // 展开数据源用于搜索
        ObservableCollection<Item> sortedItems = new ObservableCollection<Item>();
        private bool _isMultiSelectEnabled;
        public bool IsMultiSelectEnabled
        {
            get => _isMultiSelectEnabled;
            set
            {
                SetProperty(ref _isMultiSelectEnabled, value);
            }
        }
        public ObservableCollection<Item> SelectedItems { get; } = new ObservableCollection<Item>();
        private bool _isAllSelected;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectAllButtonText));
                }
            }
        }

        public string SelectAllButtonText => IsAllSelected ? "取消全选" : "全选";


        public MainViewModel()
        {
            _dataService = ((App)Application.Current).DataService;
            // 加载数据
            RefreshItems();


            CreateFolderCommand = new RelayCommand(CreateFolderFile);
            CreateNoteCommand = new RelayCommand(CreateNoteFile);
            ShowAllFoldersCommand = new RelayCommand(ShowAllFolders);
        }
        public void ToggleMultiSelect()
        {
            IsMultiSelectEnabled = !IsMultiSelectEnabled;
            SelectedItems.Clear();
        }
        public void FavoriteSelectedItems()
        {
            foreach (var item in SelectedItems)
            {
                item.IsFavorite = true;

                if (item is Note note)
                {
                    _dataService.SaveToFile(note);
                }
                else if (item is Folder folder)
                {
                    _dataService.SaveToFile(folder);
                }
            }
        }
        public void UnFavoriteSelectedItems()
        {
            foreach (var item in SelectedItems)
            {
                item.IsFavorite = false;

                if (item is Note note)
                {
                    _dataService.SaveToFile(note);
                }
                else if (item is Folder folder)
                {
                    _dataService.SaveToFile(folder);
                }
            }
        }
        public async void DeleteSelectedItems()
        {
            foreach (var item in SelectedItems.ToList()) // 防止集合在遍历中变动
            {
                if (item is Note note)
                {
                    ((App)Application.Current).DeletedNoteNames.Add(note.Name);
                    try
                    {
                        var folder = await StorageFolder.GetFolderFromPathAsync(_dataService._dataFilePath);
                        string fileName = note.Name + ".json";
                        var file = await folder.GetFileAsync(fileName);
                        await file.DeleteAsync();

                        Debug.WriteLine($"✅ 文件 {fileName} 删除成功");
                    }
                    catch (FileNotFoundException)
                    {
                        Debug.WriteLine("❌ 文件不存在，可能已被提前删除。");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 删除失败：{ex}");
                    }

                    Items.Remove(note);
                }
                else if (item is Folder folder)
                {
                    await DeleteFolderAsync(folder);
                    Items.Remove(folder);
                }
            }

            SelectedItems.Clear(); // 可选：清空多选集合
        }

        public async void RefreshItems()
        {
            Calendar = new Calendar();
            Schedule = await _dataService.LoadSchedule() ?? CreateNewSchedule();
            sortedItems = _dataService.LoadData();
            // Load other data and bind it to Items collection
            Items.Clear();  // 清除旧数据
            Items.Add(Calendar);
            Items.Add(Schedule);
            foreach (var item in sortedItems)
            {
                Items.Add(item);
            }
            // 正确地遍历 LoadData 返回的结构
            _allItemsFlat.Clear();
            foreach (var item in sortedItems)
            {
                if (item is Folder folder)
                {
                    _allItemsFlat.Add(folder);
                    foreach (var sub in folder.Items1)
                        _allItemsFlat.Add(sub);
                }
                else if (item is Note note)
                {
                    _allItemsFlat.Add(note);
                }
            }
        }
        public void ApplyFilter(string filter)
        {
            Items.Clear();
            sortedItems = _dataService.LoadData();
            _allItemsFlat.Clear();
            foreach (var item in sortedItems)
            {
                if (item is Folder folder)
                {
                    _allItemsFlat.Add(folder);
                    foreach (var sub in folder.Items1)
                        _allItemsFlat.Add(sub);
                }
                else if (item is Note note)
                {
                    _allItemsFlat.Add(note);
                }
            }
            IEnumerable<Item> result = _allItemsFlat;

            switch (filter)
            {
                case "Favorite":
                    result = _allItemsFlat.Where(i => i.IsFavorite);
                    break;
                case "Recent":
                    result = _allItemsFlat.OrderByDescending(i => i.LastAccessed).Take(10);
                    break;
                case "Note":
                    result = _allItemsFlat.Where(i => i is Note);
                    break;
                case "Folder":
                    result = _allItemsFlat.Where(i => i is Folder);
                    break;
                default:
                    break;
            }

            if (filter == "None")
            {
                Items.Add(Calendar);
                Items.Add(Schedule);
                foreach (var item in sortedItems)
                {
                    Items.Add(item);
                }
            }
            else
                foreach (var item in result)
                    Items.Add(item);
        }
        public void Search(string keyword)
        {
            keyword = keyword?.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                RefreshItems(); // 重新加载原始数据
                return;
            }

            var result = new ObservableCollection<Item>();

            foreach (var item in _allItemsFlat)
            {
                if (item is Folder folder && folder.Title?.ToLower().Contains(keyword) == true)
                {
                    result.Add(folder);
                }
                else if (item is Note note)
                {
                    if (note.Title?.ToLower().Contains(keyword) == true ||
                        note.PlainTextContent?.ToLower().Contains(keyword) == true)
                    {
                        result.Add(note);
                    }
                }
            }

            Items.Clear();
            foreach (var item in result)
                Items.Add(item);
        }

        private async void ShowAllFolders()
        {
            var items = _dataService.LoadData();
            var allFolders = GetAllFolders(items, _selectedItem);
            var availableFolders = FilterFolders(allFolders, _selectedItem);
            Folder folder1 = new Folder();
            folder1.Title = "🏠";
            availableFolders.Add(folder1);
            if (FolderSelectionDialog != null)
            {
                _selectedFolder = await FolderSelectionDialog(availableFolders);

                if (_selectedFolder != null && _selectedItem != null)
                {
                    MoveItemToFolder(_selectedItem, _selectedFolder);
                }
            }
            RefreshItems();
        }
        public void MoveItemToFolder(Item item, Folder folder)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Item 不能为空");
            }

            if (folder == null)
            {
                throw new ArgumentNullException(nameof(folder), "Folder 不能为空");
            }

            item.beFolderID = folder.Name;

            if (item is Folder _folder)
            {
                _folder.Items1.Clear();
                _dataService.SaveToFile(_folder);
            }
            else if (item is Note _note) { _dataService.SaveToFile(_note); }

        }

        public async Task DeleteFolderAsync(Folder folder)
        {
            var storageRoot = await StorageFolder.GetFolderFromPathAsync(_dataService._dataFilePath);
            await CascadeDeleteAsync(folder, storageRoot);
        }

        private async Task CascadeDeleteAsync(Item item, StorageFolder storageRoot)
        {
            if (item is Folder folder)
            {
                // 先删除所有子项
                var subitems = new List<Item>(folder.Items1);
                foreach (var subitem in subitems)
                {
                    await CascadeDeleteAsync(subitem, storageRoot);
                }
                // 然后删除自身
                await DeleteItemJsonAsync(item, storageRoot);
                RemoveFromParent(item);
            }
            else
            {
                // 对文件，直接删除自身
                await DeleteItemJsonAsync(item, storageRoot);
                RemoveFromParent(item);
            }
        }

        private async Task DeleteItemJsonAsync(Item item, StorageFolder storageRoot)
        {
            try
            {
                if (item.FileType is "Note")
                {
                    ((App)Application.Current).DeletedNoteNames.Add(item.Name);
                }
                var file = await storageRoot.GetFileAsync(item.Name + ".json");
                await file.DeleteAsync();
                Debug.WriteLine($"✅ 删除：{item.Name}.json");
            }
            catch (FileNotFoundException) { }
            catch (Exception ex) { Debug.WriteLine($"❌ 删除 {item.Name}.json 失败：{ex}"); }
        }

        private void RemoveFromParent(Item item)
        {
            if (!string.IsNullOrEmpty(item.beFolderID))
            {
                var parent = FindFolderById(Items, item.beFolderID);
                if (parent != null)
                {
                    parent.Items1.Remove(item);
                }
            }
            else
            {
                Items.Remove(item);
            }
        }

#nullable enable
        private Folder? FindFolderById(ObservableCollection<Item> items, string id)
        {
            foreach (var item in items)
            {
                if (item is Folder folder)
                {
                    if (folder.Name == id)
                        return folder;

                    var result = FindFolderById(folder.Items1, id);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }
        /// <summary>
        /// 递归遍历传入的项目集合，收集其中所有的 Folder 实例。
        /// </summary>
        /// <param name="items">当前级别的 Item 集合，其中包含 Folder 和其他类型的项。</param>
        /// <param name="excludedItem">可选参数，目前未使用，可用于后续排除逻辑。</param>
        /// <returns>返回一个包含所有 Folder 的列表。</returns>
        public List<Folder> GetAllFolders(IEnumerable<Item> items, Item? excludedItem = null)
        {
            var folders = new List<Folder>();

            foreach (var item in items)
            {
                // 只对 Folder 类型的项进行处理
                if (item is Folder folder)
                {
                    // 1. 将当前 Folder 加入结果
                    folders.Add(folder);

                    // 2. 对该 Folder 的子集合继续递归收集
                    folders.AddRange(GetAllFolders(folder.Items1, excludedItem));
                }
            }

            return folders;
        }

        /// <summary>
        /// 对所有 Folder 列表进行过滤，排除与指定项（或文件夹）相关的无效目标。
        /// </summary>
        /// <param name="folders">待过滤的所有 Folder 列表。</param>
        /// <param name="excludedItem">要排除的目标项（其自身及后代需被排除）。</param>
        /// <returns>返回一个去除了自身、同父级及后代的可选 Folder 列表。</returns>
        public List<Folder> FilterFolders(List<Folder> folders, Item excludedItem)
        {
            // 1. 构建 Name->Folder 的字典，便于父级查找
            var folderDict = folders.ToDictionary(f => f.Name);

            return folders
                .Where(folder =>
                {
                    // 2. 排除：与排除项同一父级的文件夹（兄弟节点）
                    if (folder.Name == excludedItem.beFolderID)
                        return false;

                    // 3. 如果排除项本身也是 Folder，则进一步排除其：
                    if (excludedItem is Folder excludedFolder)
                    {
                        // 3.1 排除自身
                        if (folder.Name == excludedFolder.Name)
                            return false;

                        // 3.2 排除所有后代文件夹
                        if (IsDescendant(folder, excludedFolder, folderDict))
                            return false;
                    }

                    // 4. 保留所有不在上述条件下的文件夹
                    return true;
                })
                .ToList();
        }

        /// <summary>
        /// 判断指定 folder 是否为 rootFolder（排除项）的后代。
        /// </summary>
        /// <param name="folder">要检测的候选文件夹。</param>
        /// <param name="rootFolder">用来排除自身及后代的根文件夹。</param>
        /// <param name="folderDict">所有 Folder 的字典映射（Name->Folder）。</param>
        /// <returns>如果 folder 在 rootFolder 的任意子孙层级，则返回 true；否则 false。</returns>
        public bool IsDescendant(
            Folder folder,
            Folder rootFolder,
            Dictionary<string, Folder> folderDict)
        {
            // 从当前候选文件夹的父 ID 开始，逐级向上查找
            string currentParentID = folder.beFolderID;

            while (!string.IsNullOrEmpty(currentParentID))
            {
                // 如果链条中出现 rootFolder，说明 folder 是其后代
                if (currentParentID == rootFolder.Name)
                    return true;

                // 沿着父级继续往上找
                if (folderDict.TryGetValue(currentParentID, out var parentFolder))
                {
                    currentParentID = parentFolder.beFolderID;
                }
                else
                {
                    // 找不到对应父节点，跳出循环
                    break;
                }
            }

            return false;
        }
        public async void MoveSelectedItemsToFolder()
        {
            if (SelectedItems == null || SelectedItems.Count == 0)
                return;

            var allItems = _dataService.LoadData();
            var allFolders = GetAllFolders(allItems);

            // 多选时可能选中了多个 folder，所以我们要排除多个文件夹的后代
            var selectedFolders = SelectedItems.OfType<Folder>().ToList();
            var excludedNames = new HashSet<string>(selectedFolders.Select(f => f.Name));

            // 找出所有需要排除的 folder（包括自身和其所有后代）
            foreach (var folder in selectedFolders)
            {
                var descendants = GetDescendantFolderNames(folder, allFolders);
                foreach (var desc in descendants)
                {
                    excludedNames.Add(desc);
                }
            }

            // 过滤出允许作为目标的 Folder
            var availableFolders = allFolders
                .Where(folder => !excludedNames.Contains(folder.Name))
                .ToList();

            // 添加根目录选项
            Folder rootFolder = new Folder { Title = "🏠", Name = string.Empty };
            availableFolders.Add(rootFolder);

            if (FolderSelectionDialog != null)
            {
                var targetFolder = await FolderSelectionDialog(availableFolders);
                if (targetFolder == null)
                    return;

                foreach (var item in SelectedItems)
                {
                    // 忽略不能移动到自身或其子项的 folder
                    if (item is Folder folder)
                    {
                        if (folder.Name == targetFolder.Name || IsDescendant(targetFolder, folder, allFolders.ToDictionary(f => f.Name)))
                            continue;
                    }

                    item.beFolderID = targetFolder.Name;

                    if (item is Folder f)
                    {
                        f.Items1.Clear();
                        _dataService.SaveToFile(f);
                    }
                    else if (item is Note n)
                    {
                        _dataService.SaveToFile(n);
                    }
                }

                RefreshItems();
            }
        }

        /// <summary>
        /// 获取某个 Folder 的所有后代 Folder 的 Name 列表
        /// </summary>
        private List<string> GetDescendantFolderNames(Folder root, List<Folder> allFolders)
        {
            var result = new List<string>();
            var dict = allFolders.ToDictionary(f => f.Name);
            var stack = new Stack<Folder>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                foreach (var child in allFolders.Where(f => f.beFolderID == current.Name))
                {
                    result.Add(child.Name);
                    stack.Push(child);
                }
            }

            return result;
        }
        public void ToggleSelectTopLevelItems()
        {
            // 获取所有顶层项（不在文件夹内）
            var topLevelItems = Items.Where(i => string.IsNullOrEmpty(i.beFolderID)).ToList();

            bool alreadyAllSelected = topLevelItems.All(item => SelectedItems.Contains(item));

            SelectedItems.Clear();

            if (!alreadyAllSelected)
            {
                foreach (var item in topLevelItems)
                {
                    SelectedItems.Add(item);
                }
            }
            else
            {
                SelectedItems.Clear();
            }
        }


        private void CreateFolderFile()
        {
            var newFolder = _dataService.CreateFolder();
            Items.Add(newFolder);
        }
        private void CreateNoteFile()
        {
            var newNote = _dataService.CreateNote();
            Items.Add(newNote);

        }

        private Schedule CreateNewSchedule()
        {
            var schedule = new Schedule()
            {
                Name = "Schedule",
                // 初始化其他属性
            };
            _dataService.SaveToFile(schedule);
            return schedule;
        }

    }
}