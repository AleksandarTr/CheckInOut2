using System;
using System.Collections.Generic;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

class ActiveEmployeesWindowViewModel : ObservableObject {
    private DatabaseInterface db;
    private ActiveEmployeesWindow activeEmployeesWindow;
    
    public ActiveEmployeesWindowViewModel(DatabaseInterface db, ActiveEmployeesWindow view) {
        this.db = db;
        activeEmployeesWindow = view;
        List<String> activeEmployees = db.getActiveEmployees(DateTime.Now.AddDays(-1));
        foreach (String employee in activeEmployees) activeEmployeesWindow.addActiveEmployee(employee);
    }
}