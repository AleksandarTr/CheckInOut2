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

    public void addReaderEventHandler(ReaderEventHandler handler) {
        readerInput += handler;
    }

    public bool ShowActiveEmplyees() {
        ActiveEmployeesWindow activeEmployeesWindow = new ActiveEmployeesWindow(db);
        activeEmployeesWindow.Show();
        return true;
    }

    public MainWindowViewModel() {
        db = new DatabaseInterface("checkIO.db");
        List<String> employees = db.getActiveEmployees(DateTime.Now.AddDays(-1));

    }
}
