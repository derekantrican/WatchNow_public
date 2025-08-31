using System;

namespace WatchNow.Avalonia.ViewModels
{
	public class ScreenSnapViewModel : ViewModelBase
	{
		private string header;
		private Action snapToScreenAction;

		public ScreenSnapViewModel(string header, Action snapToScreenAction)
		{
			this.header = header;
			this.snapToScreenAction = snapToScreenAction;
		}

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

		public void SnapToScreen()
		{
			snapToScreenAction();
		}
	}
}
