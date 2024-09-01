using System;
using Avalonia.Controls;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

public delegate bool ReaderEventHandler(String message);

public partial class MainWindowViewModel : ObservableObject
{
    private DatabaseInterface db;
    static private bool adminPanelOpen = false;
    static private bool showActiveEmplyeesOpen = false;

    static public void adminPanelClosed() {
        adminPanelOpen = false;
    }

    static public void showActiveEmplyeesClosed() {
        showActiveEmplyeesOpen = false;
    }

    public void showActiveEmplyees() {
        if(showActiveEmplyeesOpen) return;
        showActiveEmplyeesOpen = true;  
        new ActiveEmployeesWindow(db);
    }

    public MainWindowViewModel(DatabaseInterface db) {
        this.db = db;
    }

    public void logIn(Window view) {
        if(adminPanelOpen) return;
        LogInWindow logInWindow = new LogInWindow(db);
        logInWindow.Show(view);
        adminPanelOpen = true;
    }

    public void readChip() {
        ChipReader.readChip("123456789");
    }
}
