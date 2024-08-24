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
        IClassicDesktopStyleApplicationLifetime desktop = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop?.Shutdown();
    }

    public void addWorker() {
        AddWorkerWindow addWorkerWindow = new AddWorkerWindow(db);
        addWorkerWindow.Show(view);
    }

    public AdminWindowViewModel(AdminWindow view, DatabaseInterface db) {
        this.view = view;
        this.db = db;
    }
}