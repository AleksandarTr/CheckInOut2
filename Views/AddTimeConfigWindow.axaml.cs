using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AddTimeConfigWindow : Window {
    public AddTimeConfigWindow(Action onClosing) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AddTimeConfigWindowViewModel();
        Closing += (o, e) => onClosing();
    }
}