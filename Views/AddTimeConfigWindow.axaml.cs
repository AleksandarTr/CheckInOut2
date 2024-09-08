using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AddTimeConfigWindow : Window {
    public AddTimeConfigWindow(Action onClosing, DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AddTimeConfigWindowViewModel(db);
        Closing += (o, e) => onClosing();
    }
}