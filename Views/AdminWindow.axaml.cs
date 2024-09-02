using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AdminWindow : Window {
    private DatabaseInterface db;

    public void closeAppClick(object sender, RoutedEventArgs args) {
        Logger.log("Manual application shutdown");
        IClassicDesktopStyleApplicationLifetime desktop = (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!;
        desktop?.Shutdown();
    }

    public void addWorkerClick(object sender, RoutedEventArgs args) {
        AddWorkerWindow addWorkerWindow = new AddWorkerWindow(db);
        addWorkerWindow.Show(this);
    }

    public void editWorkerClick(object sender, RoutedEventArgs args) {
        EditWorkerWindow editWorkerWindow = new EditWorkerWindow(db);
        editWorkerWindow.Show(this);
    }

    public void checkWorkerClick(object sender, RoutedEventArgs args) {
        WorkerCheckWindow workerCheckWindow = new WorkerCheckWindow(db);
        workerCheckWindow.Show(this);
    }

    public void editCheckClick(object sender, RoutedEventArgs args) {
        EditCheckWindow editCheckWindow = new EditCheckWindow(db);
        editCheckWindow.Show(this);
    }

    public void exportClick(object sender, RoutedEventArgs args) {
        ExportWindow exportWorkerWindow = new ExportWindow(db);
        exportWorkerWindow.Show(this);
    }

    public void addUserClick(object sender, RoutedEventArgs args) {
        AddUserWindow addUserWindow = new AddUserWindow(db);
        addUserWindow.Show(this);
    }

    public void editUserClick(object sender, RoutedEventArgs args) {
        EditUserWindow editUserWindow = new EditUserWindow(db);
        editUserWindow.Show(this);
    }

    public AdminWindow(int permission, DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        this.db = db;

        if((permission & 1) == 0) this.FindControl<Button>("addWorker")!.IsEnabled = false;
        if((permission & 2) == 0) this.FindControl<Button>("checkWorker")!.IsEnabled = false;
        if((permission & 4) == 0) this.FindControl<Button>("editWorker")!.IsEnabled = false;
        if((permission & 8) == 0) this.FindControl<Button>("exportActivity")!.IsEnabled = false;
        if((permission & 16) == 0) this.FindControl<Button>("editCheck")!.IsEnabled = false;
        if((permission & 32) == 0) this.FindControl<Button>("addUser")!.IsEnabled = false;
        if((permission & 64) == 0) this.FindControl<Button>("editUser")!.IsEnabled = false;
        if((permission & 128) == 0) this.FindControl<Button>("closeProgram")!.IsEnabled = false;

        Closing += (sender, e) => {
            MainWindowViewModel.adminPanelClosed();
            Logger.log("AdminWindow closed");
        };
        Logger.log("AdminWindow opened");
    }
}