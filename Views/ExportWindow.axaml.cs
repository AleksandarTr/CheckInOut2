using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class ExportWindow : Window {
    public ExportWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ExportWindowViewModel(db);
        Logger.log("ExportWindow opened");
        Closing += (s, o) => Logger.log("ExportWindow closed");
    }
}