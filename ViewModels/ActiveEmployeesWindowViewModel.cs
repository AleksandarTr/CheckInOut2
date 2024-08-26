using System;
using System.Collections.Generic;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class ActiveEmployeesWindowViewModel : ObservableObject {
    private DatabaseInterface db;
    private ActiveEmployeesWindow activeEmployeesWindow;
    
    public ActiveEmployeesWindowViewModel(DatabaseInterface db, ActiveEmployeesWindow view, MainWindow mainWindow) {
        this.db = db;
        activeEmployeesWindow = view;
        List<String> activeEmployees = db.getActiveEmployees(DateTime.Now);
        if(activeEmployees.Count == 0) {
            MessageBoxManager.GetMessageBoxStandard("Nema radnika", "Trenutno nijedan radnik nije na poslu.", 
            MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            activeEmployeesWindow.Close();
        }
        else {
            foreach (String employee in activeEmployees) activeEmployeesWindow.addActiveEmployee(employee);
            activeEmployeesWindow.Show(mainWindow);
        }
    }
}