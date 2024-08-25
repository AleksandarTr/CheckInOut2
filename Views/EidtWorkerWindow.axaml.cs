using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class EditWorkerWindow : Window {
    public EditWorkerWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new EditWorkerWindowViewModel(db);
        SizeToContent = SizeToContent.WidthAndHeight;
    }
}