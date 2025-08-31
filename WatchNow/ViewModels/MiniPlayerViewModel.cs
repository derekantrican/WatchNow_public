using System;
using System.Linq;

namespace WatchNow.Avalonia.ViewModels
{
	public class MiniPlayerViewModel : ViewModelBase
	{
		private int progress;
		private int progressMax;

		public MiniPlayerViewModel(MainViewModel mainViewModel)
		{
			MainViewModel = mainViewModel;
		}

		public int NewItemsCount
		{
			get
			{
				return MainViewModel.Sources.SelectMany(s => s.Items.Where(i => i.NewLabelVisible)).Count();
			}
		}

		public int Progress
		{
			get
			{
				return progress;
			}
			set
			{
				progress = value;
				FirePropertyChanged();
			}
		}

		public int ProgressMax
		{
			get
			{
				return progressMax;
			}
			set
			{
				progressMax = value;
				FirePropertyChanged();
			}
		}

		public MainViewModel MainViewModel { get; set; }

		public Action SwitchToMainPlayerAction { get; set; }
		public Action CloseWindowAction { get; set; }

		public void SwitchToMainPlayer()
		{
			SwitchToMainPlayerAction();
		}

		public void CloseWindow()
		{
			CloseWindowAction();
		}

		public void Refresh()
		{
			MainViewModel.Refresh();
		}

		public void RefreshAll()
		{ 
			MainViewModel.RefreshAll();
		}

		public void PlayPause()
		{
			MainViewModel.PlayPause();
		}
	}
}
