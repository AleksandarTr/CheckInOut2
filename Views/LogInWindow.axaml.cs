using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class LogInWindow : Window {
    public LogInWindow(DatabaseInterface db, MainWindow mainWindow) {
        AvaloniaXamlLoader.Load(this);
        DataContext= new LogInWindowViewModel(db, this, mainWindow);
        SizeToContent = SizeToContent.WidthAndHeight;
        TextBlock chip = this.FindControl<TextBlock>("chip");
        chip.Text = "Čip:  ";
        Closing += (sender, e) => (mainWindow.DataContext as MainWindowViewModel).adminPanelClosed();
    }
}