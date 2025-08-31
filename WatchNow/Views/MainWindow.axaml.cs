using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using MsBox.Avalonia;
using WatchNow.Avalonia.ViewModels;
using WatchNow.Helpers;

namespace WatchNow.Avalonia.Views;

public partial class MainWindow : Window
{
    private MainViewModel mainViewModel;
	private MiniPlayer miniPlayer;
	private bool webViewFullScreen;

	private PixelPoint lastPosition;
	private double lastWidth;
	private double lastHeight;
	private double lastWebViewWidth;
	private double lastWebViewHeight;

	public MainWindow(MainViewModel viewModel) : this()
	{
		mainViewModel = viewModel;
		this.DataContext = viewModel;
		miniPlayer = new MiniPlayer(this, mainViewModel.MiniPlayerViewModel);

		Common.ShowMessageBox = (title, message) => Dispatcher.UIThread.Invoke(() => MessageBoxManager.GetMessageBoxStandard(title, message).ShowWindowDialogAsync(this));
		viewModel.GetWindowSizeAndPosition = () => (this.Position.X, this.Position.Y, (int)this.Width, (int)this.Height);
		viewModel.EscExitFullScreenAction += ExitFullScreen;
		viewModel.OpenWebViewDevToolsAction = () => webView.OpenDevToolsWindow();
		viewModel.PlayPauseAction += PlayPause;
		viewModel.SwitchToMiniPlayerAction = ShowMiniPlayer;
        viewModel.MinimizeAction = () => this.WindowState = WindowState.Minimized;
		viewModel.CloseAction = () =>
		{
			//Need to close both
			miniPlayer.Close();
			this.Close();
		};
	}

	public MainWindow()
	{
		InitializeComponent();

		this.Loaded += MainWindow_Loaded;
		this.Opened += (sender, args) => WinApi.RemoveRoundedCorners(this.TryGetPlatformHandle().Handle);
		this.GotFocus += (sender, args) => WinApi.RemoveRoundedCorners(this.TryGetPlatformHandle().Handle);
	}

	private void MainWindow_Loaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		InitWindowSize();

		LoadScreenSnapOptions();

		mainViewModel.LoadSources();
	}

	private void ContextMenu_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
	{
		// https://github.com/AvaloniaUI/Avalonia/issues/16855
		LoadScreenSnapOptions();
	}

	private void LoadScreenSnapOptions()
	{
		if (!Design.IsDesignMode)
		{
			mainViewModel.ScreenSnapOptions.Clear();

			foreach (WinApi.DisplayInfo displayInfo in WinApi.GetDisplayDevices())
			{
				mainViewModel.ScreenSnapOptions.Add(new ScreenSnapViewModel(displayInfo.DisplayName,
				() =>
				{
					this.Position = new PixelPoint(
						displayInfo.WorkingArea.left + displayInfo.WorkingArea.width - (int)this.Width - 1, // "-1" because for some reason this seems to be a pixel off (and for some reason it's only the width - not height)
						displayInfo.WorkingArea.top + displayInfo.WorkingArea.height - (int)this.Height
					);
				}));
			}
		}
	}

	private void WebView_NavigationCompleted(object sender, WebViewCore.Events.WebViewUrlLoadedEventArg e)
	{
		// Todo: similar to "adding a sponsorblock button" like the below function, we should also add a "show as embed" button that redirects a YouTube url (eg https://www.youtube.com/watch?v=KC-NOkm-dGs) to the embed version (eg https://www.youtube.com/embed/KC-NOkm-dGs)
		webView.ExecuteScriptAsync(mainViewModel.GetSponsorBlockJSForCurrentUrl());
	}

	private void WebView_FullScreenChanged(object sender, WebViewCore.Events.WebViewFullScreenChangedEventArgs e)
	{
		//Adding this event required me to build a custom version of Avalonia.WebView because the official
		//project seems to be inactive now (and my PR isn't getting considered).
		//https://github.com/derekantrican/Avalonia.WebView

		webViewFullScreen = e.IsFullScreen;

		if (e.IsFullScreen)
		{
			//Save window size & position (for restore)
			lastPosition = this.Position;
			lastWidth = this.Width;
			lastHeight = this.Height;

			this.WindowState = WindowState.FullScreen;

			//For fullscreen mode, hide non-player elements
			tabStrip.IsVisible = false;
			progressBar.IsVisible = false;
		}
        else
        {
			this.WindowState = WindowState.Normal;

			//Show the non-player elements again
			tabStrip.IsVisible = true;
			progressBar.IsVisible = true;

			//Restore window size & position (I think hiding & showing the tabStrip & progressBar messes with
			//Window's ability to restore the correct size & position of the form so we will do it ourselves)
			this.Position = lastPosition;
			this.Width = lastWidth;
			this.Height = lastHeight;
			//Todo: somehow the webView's video (not the webView itself) is *slightly* undersized (a few pixels of black on the left side).
			// (Or maybe it's that the webView thinks it's supposed to be slightly wider than it actually is?)
			//  Maybe we can invoke some sort of JS function to update the size of the video
			WinApi.RemoveRoundedCorners(this.TryGetPlatformHandle().Handle); //This also needs to be done again

			//Window will have lost its "TopMost" ability after being restored https://github.com/AvaloniaUI/Avalonia/issues/2841
			//Need to "turn it off & on again" to refresh the Topmost property (probably something is not run if the window
			//thinks it is already topmost)
			this.Topmost = false;
			this.Topmost = true;
		}
    }

	private void ExitFullScreen()
	{
		if (webViewFullScreen)
		{
			webView.ExecuteScriptAsync("document.exitFullscreen();");
		}
	}

	private void PlayPause()
	{
		webView.ExecuteScriptAsync("""
        var video = document.querySelector("video");
        if (video.paused) {
          video.play();
        }
        else {
          video.pause();
        }
        """);
	}

	private void ShowMiniPlayer()
	{
		miniPlayer.Show();
		miniPlayer.Position = new PixelPoint(this.Position.X + (int)this.Width - (int)miniPlayer.Width, this.Position.Y + (int)this.Height - (int)miniPlayer.Height);
		WinApi.RemoveRoundedCorners(miniPlayer.TryGetPlatformHandle().Handle); //Need to do this again (doing it here because there appears to be no Window.Shown event in Avalonia)
		this.Hide();
	}


	//Todo: it might be better in the future if we just had a single toolbar "button" (a "drag handle") for click-drag moving the application around (instead of just "anywhere on the window")
	private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;

    //Credit: https://github.com/AvaloniaUI/Avalonia/discussions/8441
    private void InputElement_OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving)
            return;

        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }

    private void InputElement_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
            return;

		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			return;
		}
        
		_mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }

    private void InputElement_OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _mouseDownForWindowMoving = false;
    }

    private void InitWindowSize()
    {
		this.Position = new PixelPoint(Common.ReadSetting("FormLeft", 0), Common.ReadSetting("FormTop", 0));
		this.Width = Common.ReadSetting("FormWidth", this.Width);
		this.Height = Common.ReadSetting("FormHeight", this.Height);

		//If the form is accidentally tiny, make it bigger
		if (this.Width < 200)
		{
			this.Width = 200;
		}

		if (this.Height < 200)
		{
			this.Height = 200;
		}

		//Move the window if it's not visible on any screen
		bool visible = false;
		foreach (Screen screen in Screens.All)
		{
			if (screen.WorkingArea.Contains(this.Position))
			{
				visible = true;
				break;
			}
		}

		if (!visible)
		{
			this.Position = new PixelPoint(Screens.Primary.WorkingArea.X, Screens.Primary.WorkingArea.Y);
		}
	}
}
