using Avalonia.Controls;
using WatchNow.Avalonia.ViewModels;

namespace WatchNow.Avalonia.Views.UserControls
{
    public partial class VideoListing : UserControl
    {
        public VideoListing()
        {
            InitializeComponent();
        }

		private void VideoListing_PointerPressed(object sender, global::Avalonia.Input.PointerPressedEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				(this.DataContext as VideoListingViewModel).LoadVideoAction();
			}

			e.Handled = true; //Prevent PointerPressed here from causing MainWindow.PointerPressed (for click-drag moving the whole window) to be triggered
		}
	}
}
