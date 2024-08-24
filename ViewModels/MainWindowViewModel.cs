using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

public delegate bool ReaderEventHandler(String message);

public partial class MainWindowViewModel : ObservableObject
{
    private event ReaderEventHandler? readerInput;
    private DatabaseInterface db;
    private bool adminPanelOpen = false;
    private bool showActiveEmplyeesOpen = false;
    private MainWindow view;

    public void adminPanelClosed() {
        adminPanelOpen = false;
    }

    public void showActiveEmplyeesClosed() {
        showActiveEmplyeesOpen = false;
    }

    public void addReaderEventHandler(ReaderEventHandler handler) {
        readerInput += handler;
    }

    public void showActiveEmplyees() {
        if(showActiveEmplyeesOpen) return;
        ActiveEmployeesWindow activeEmployeesWindow = new ActiveEmployeesWindow(db, this);
        activeEmployeesWindow.Show(view);
        showActiveEmplyeesOpen = true;
    }

    public MainWindowViewModel(MainWindow view) {
        db = new DatabaseInterface("checkIO.db");
        List<String> employees = db.getActiveEmployees(DateTime.Now.AddDays(-1));
        this.view = view;
    }

    public void logIn() {
        if(adminPanelOpen) return;
        LogInWindow logInWindow = new LogInWindow(db, view);
        logInWindow.Show(view);
        adminPanelOpen = true;
    }
}
