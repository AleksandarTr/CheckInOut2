using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AddUserWindow : Window {
    public AddUserWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AddUserWindowViewModel(db);
        SizeToContent = SizeToContent.WidthAndHeight;
    }
}