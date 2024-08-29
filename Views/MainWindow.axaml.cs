using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

public partial class MainWindow : Window
{
    static private MainWindow? _instance;
    static public MainWindow instance { get {return _instance!;} }

    public bool addMessage(String message) {
        StackPanel? infoBoard = this.FindControl<StackPanel>("informationBoard");
        if(infoBoard == null) return false;
        infoBoard.Children.Add(new TextBlock{Text = message});
        return true;
    }

    protected override void OnClosing(WindowClosingEventArgs e) {
        e.Cancel = true;
        base.OnClosing(e);
    }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        MainWindowViewModel viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        _instance = this;
    }
}