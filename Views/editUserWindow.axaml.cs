using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class EditUserWindow : Window {
    public EditUserWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new EditUserWindowViewModel(db);
        SizeToContent = SizeToContent.WidthAndHeight;
    }
}