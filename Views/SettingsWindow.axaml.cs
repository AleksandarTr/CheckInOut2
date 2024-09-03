using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class SettingsWindow : Window {
    public SettingsWindow() {
        AvaloniaXamlLoader.Load(this);
        DataContext = new SettingsWindowViewModel();
        Logger.log("SettingsWindow opened");
        Closing += (o, e) => Logger.log("SettingsWindow closed");
    }
}