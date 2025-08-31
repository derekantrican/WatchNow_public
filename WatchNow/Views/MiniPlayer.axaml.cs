using Avalonia.Controls;
using WatchNow.Avalonia.ViewModels;
using WatchNow.Avalonia.Views;
using WatchNow.Helpers;

namespace WatchNow.Avalonia;

public partial class MiniPlayer : Window
{
	private MainWindow parent;

	public MiniPlayer(MainWindow parent, MiniPlayerViewModel viewModel) : this()
	{
		this.parent = parent;
		this.DataContext = viewModel;

		viewModel.SwitchToMainPlayerAction += SwitchToMainPlayer;
		viewModel.CloseWindowAction += () =>
		{
			//MainViewModel's CloseWindowAction (called by CloseWindow) will close both the main & mini players
			viewModel.MainViewModel.CloseWindow(); //Calling CloseWindow will trigger saving of MainWindow size, position, & sources
		};
	}

	public MiniPlayer()
	{
		InitializeComponent();

		this.Opened += (sender, args) => WinApi.RemoveRoundedCorners(this.TryGetPlatformHandle().Handle);
	}

	private void SwitchToMainPlayer()
	{
		parent.Show();
		WinApi.RemoveRoundedCorners(parent.TryGetPlatformHandle().Handle); //Need to do this again (doing it here because there appears to be no Window.Shown event in Avalonia)
		this.Hide();
	}
}