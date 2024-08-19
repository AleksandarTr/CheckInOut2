using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

public partial class MainWindow : Window
{
    public bool addMessage(String message) {
        StackPanel? infoBoard = this.FindControl<StackPanel>("informationBoard");
        if(infoBoard == null) return false;
        infoBoard.Children.Add(new TextBlock{Text = message});
        return true;
    }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        MainWindowViewModel viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        viewModel.addReaderEventHandler(new ReaderEventHandler(addMessage));
    }
}