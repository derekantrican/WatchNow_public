using System;

namespace WatchNow.Avalonia.ViewModels
{
	public class VideoListingViewModel
	{
		//Todo: now that I've moved to MVVM, I don't think all these properties are necessary (eg 'Source' can probably be removed?)
		public string Text { get; set; }
		public string ImageUrl { get; set; }
		public Action LoadVideoAction { get; set; }
		public bool ActionButtonVisible { get; set; }
		public string ActionButtonText { get; set; }
		public Action ActionButtonAction { get; set; }
		public Action OpenInBrowserAction { get; set; }
		public bool NewLabelVisible { get; set; }
	}
}
