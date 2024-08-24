using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AddWorkerWindow : Window {
    public AddWorkerWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AddWorkerWindowViewModel(db);
        SizeToContent = SizeToContent.WidthAndHeight;
    }
}