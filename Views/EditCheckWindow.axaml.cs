using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class EditCheckWindow : Window {
    public EditCheckWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        SizeToContent = SizeToContent.WidthAndHeight;
        DataContext = new EditCheckWindowViewModel(db, this);
    }
}