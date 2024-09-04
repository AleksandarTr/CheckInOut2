using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

public partial class MainWindow : Window
{
    private DatabaseInterface db;
    private static MainWindow instance;

    public static bool addMessage(String message) {
        Logger.log("Added message:" + message);
        StackPanel? infoBoard = instance.FindControl<StackPanel>("informationBoard");
        if(infoBoard == null) return false;
        infoBoard.Children.Add(new TextBlock{Text = message});
        return true;
    }

    private void onChipRead(string chip) {
        if(!ChipReader.isFocused(this)) return;
        addMessage(db.logCheckIn(chip, DateTime.Now));
    }

    protected override void OnClosing(WindowClosingEventArgs e) {
        e.Cancel = true;
        Logger.log($"Application closed at {DateTime.Now:dd.MM.yyyy-HH:mm}");
        base.OnClosing(e);
    }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        db = new DatabaseInterface("checkIO.db");
        MainWindowViewModel viewModel = new MainWindowViewModel(db);
        DataContext = viewModel;
        instance = this;

        ChipReader.focusWindow(this);
        ChipReader.addChipReaderEventHandler(onChipRead);
        Logger.log("Main window opened");
    }
}