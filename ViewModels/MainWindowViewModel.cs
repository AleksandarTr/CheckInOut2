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
    private DatabaseInterface db;
    private bool adminPanelOpen = false;
    private bool showActiveEmplyeesOpen = false;

    public void adminPanelClosed() {
        adminPanelOpen = false;
    }

    public void showActiveEmplyeesClosed() {
        showActiveEmplyeesOpen = false;
    }

    public void showActiveEmplyees() {
        if(showActiveEmplyeesOpen) return;
        showActiveEmplyeesOpen = true;  
        new ActiveEmployeesWindow(db);
    }

    private void onChipRead(string chip) {
        if(!ChipReader.isFocused(MainWindow.instance)) return;
        MainWindow.instance.addMessage(db.logCheckIn(chip, DateTime.Now));
    }

    public MainWindowViewModel() {
        db = new DatabaseInterface("checkIO.db");
        List<String> employees = db.getActiveEmployees(DateTime.Now.AddDays(-1));
        ChipReader.focusWindow(MainWindow.instance);
        ChipReader.addChipReaderEventHandler(onChipRead);
    }

    public void logIn() {
        if(adminPanelOpen) return;
        LogInWindow logInWindow = new LogInWindow(db);
        logInWindow.Show(MainWindow.instance);
        adminPanelOpen = true;
    }

    public void readChip() {
        ChipReader.readChip("123456789");
    }
}
