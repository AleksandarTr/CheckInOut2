using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
        Border border = new Border() {
            BorderBrush = SolidColorBrush.Parse("Gray"),
            BorderThickness = Thickness.Parse("0 0 0 1"),
            Child = new TextBlock{
                Text = message,
                FontSize = int.Parse(Settings.get("fontSize")!),
                Margin = Thickness.Parse("10 5 0 5")
            }
        };
        infoBoard.Children.Add(border);
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