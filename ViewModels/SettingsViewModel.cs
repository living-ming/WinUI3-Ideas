using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IDEAs.Services;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IDEAs.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly DataService _dataService;
        public double BackgroundOpacity
        {
            get => _dataService.BackgroundOpacity;
            set
            {
                if (_dataService.BackgroundOpacity != value)
                {
                    _dataService.BackgroundOpacity = value;
                    OnPropertyChanged(nameof(BackgroundOpacity));
                }
            }
        }
        public bool ShowWordCount
        {
            get => _dataService.ShowWordCount;
            set
            {
                if (_dataService.ShowWordCount != value)
                {
                    _dataService.ShowWordCount = value;
                    OnPropertyChanged(nameof(ShowWordCount));
                }
            }
        }

        public bool HighlightComments
        {
            get => _dataService.HighlightComments;
            set
            {
                if (_dataService.HighlightComments != value)
                {
                    _dataService.HighlightComments = value;
                    OnPropertyChanged(nameof(HighlightComments));
                }
            }
        }

        public SettingsViewModel()
        {
            _dataService = ((App)Application.Current).DataService;
            SetCustomSavePathCommand = new AsyncRelayCommand(SetCustomSavePathAsync);
            SetCustomBackgroundPathCommand = new AsyncRelayCommand(SetCustomBackgroundPathAsync);
        }

        public ICommand SetCustomSavePathCommand { get; }
        public ICommand SetCustomBackgroundPathCommand { get; }

        private async Task SetCustomBackgroundPathAsync()
        {
            await _dataService.SetCustomBackgroundPathAsync();
        }
        private async Task SetCustomSavePathAsync()
        {
            await _dataService.SetCustomSavePathAsync();
        }
    }
}
