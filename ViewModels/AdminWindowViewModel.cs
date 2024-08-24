using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

class AdminWindowViewModel : ObservableObject {
    public void closeApp() {
        IClassicDesktopStyleApplicationLifetime desktop = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop?.Shutdown();
    }

    public AdminWindowViewModel() {
    }
}