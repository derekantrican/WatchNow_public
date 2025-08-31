using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WatchNow.Avalonia.ViewModels
{
    public class SourceViewModel : ViewModelBase
    {
        private ObservableCollection<VideoListingViewModel> items = new ObservableCollection<VideoListingViewModel>();

        private string header;
        public string Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
                FirePropertyChanged();
            }
        }
        public string UrlToOpen { get; set; }
        public bool HasNewItems
        {
            get
            {
                return Items.Any(i => i.NewLabelVisible);
            }
        }
        public bool CanBeSerialized { get; set; } = true;
        public Action RemoveSourceAction { get; set; }

        public ObservableCollection<VideoListingViewModel> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
                FirePropertyChanged();
            }
        }

        public virtual void Auth() { }
        public virtual Task LoadItems(int amount, Action<int> addVideoCount, Action incrementProgressBar, Action<string> loadVideoAction) { return Task.CompletedTask; }

        public void RemoveSource()
        {
            RemoveSourceAction();
        }
    }
}
