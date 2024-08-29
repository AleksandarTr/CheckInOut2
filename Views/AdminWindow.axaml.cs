using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AdminWindow : Window {
    public AdminWindow(int permission, DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AdminWindowViewModel(this, db);

        if((permission & 1) == 0) this.FindControl<Button>("addWorker")!.IsEnabled = false;
        if((permission & 2) == 0) this.FindControl<Button>("checkWorker")!.IsEnabled = false;
        if((permission & 4) == 0) this.FindControl<Button>("editWorker")!.IsEnabled = false;
        if((permission & 8) == 0) this.FindControl<Button>("exportActivity")!.IsEnabled = false;
        if((permission & 16) == 0) this.FindControl<Button>("editCheck")!.IsEnabled = false;
        if((permission & 32) == 0) this.FindControl<Button>("addUser")!.IsEnabled = false;
        if((permission & 64) == 0) this.FindControl<Button>("editUser")!.IsEnabled = false;
        if((permission & 128) == 0) this.FindControl<Button>("closeProgram")!.IsEnabled = false;

        Closing += (sender, e) => (MainWindow.instance.DataContext as MainWindowViewModel)!.adminPanelClosed();
    }
}