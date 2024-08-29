using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class LogInWindow : Window {
    private void onChipRead(string chip) {
        if(!ChipReader.isFocused(this)) return;
        TextBlock? chipBlock = this.FindControl<TextBlock>("chip");
        chipBlock!.Text = $"Čip: {chip}";
    }

    public LogInWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext= new LogInWindowViewModel(db, this);

        ChipReader.addChipReaderEventHandler(onChipRead);
        ChipReader.focusWindow(this);
        Closing += (sender, e) => {
            (MainWindow.instance.DataContext as MainWindowViewModel).adminPanelClosed();
            ChipReader.removeChipReaderEventHandler(onChipRead);
            ChipReader.focusWindow(MainWindow.instance);
        };
    }
}