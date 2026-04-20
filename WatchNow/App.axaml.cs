using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using WatchNow.Avalonia.ViewModels;
using WatchNow.Avalonia.Views;

namespace WatchNow.Avalonia;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new MainWindow(new MainViewModel());
		}

		base.OnFrameworkInitializationCompleted();
	}
}
