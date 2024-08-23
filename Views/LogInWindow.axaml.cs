using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class LogInWindow : Window {
    public LogInWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext= new LogInWindowViewModel(db, this);
        SizeToContent = SizeToContent.WidthAndHeight;
        TextBlock chip = this.FindControl<TextBlock>("chip");
        chip.Text = "Čip:  ";
    }
}