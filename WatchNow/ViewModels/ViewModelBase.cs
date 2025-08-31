using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WatchNow.Avalonia.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public void FirePropertyChanged([CallerMemberName] string aPropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(aPropertyName));
    }
}
