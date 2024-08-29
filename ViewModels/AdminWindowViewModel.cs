using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

class AdminWindowViewModel : ObservableObject {
    private AdminWindow view;
    private DatabaseInterface db;

    public void closeApp() {
        IClassicDesktopStyleApplicationLifetime desktop = (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!;
        desktop?.Shutdown();
    }

    public void addWorker() {
        AddWorkerWindow addWorkerWindow = new AddWorkerWindow(db);
        addWorkerWindow.Show(view);
    }

    public void editWorker() {
        EditWorkerWindow editWorkerWindow = new EditWorkerWindow(db);
        editWorkerWindow.Show(view);
    }

    public void checkWorker() {
        WorkerCheckWindow workerCheckWindow = new WorkerCheckWindow(db);
        workerCheckWindow.Show(view);
    }

    public void editCheck() {
        EditCheckWindow editCheckWindow = new EditCheckWindow(db);
        editCheckWindow.Show(view);
    }

    public void export() {
        ExportWindow exportWorkerWindow = new ExportWindow(db);
        exportWorkerWindow.Show(view);
    }

    public void addUser() {
        AddUserWindow addUserWindow = new AddUserWindow(db);
        addUserWindow.Show(view);
    }

    public void editUser() {
        EditUserWindow editUserWindow = new EditUserWindow(db);
        editUserWindow.Show(view);
    }

    public AdminWindowViewModel(AdminWindow view, DatabaseInterface db) {
        this.view = view;
        this.db = db;
    }
}