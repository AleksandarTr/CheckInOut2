using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class EditWorkerWindow : Window {
    private void onChipRead(string chip) {
        if(!ChipReader.isFocused(this)) return;
        TextBlock? chipBlock = this.FindControl<TextBlock>("chip");
        chipBlock!.Text = $"Čip: {chip}";
    }

    public EditWorkerWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new EditWorkerWindowViewModel(db);

        ChipReader.addChipReaderEventHandler(onChipRead);
        ChipReader.focusWindow(this);
        Closing += (o, sender) => {
            ChipReader.removeChipReaderEventHandler(onChipRead);
            ChipReader.unfocus();
            Logger.log("EditWorkerWindow closed");
        };

        Logger.log("EditWorkerWindow opened");
    }
}