using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;
using MsBox.Avalonia;

namespace CheckInOut2.Views;

partial class ActiveEmployeesWindow : Window {
    public void addActiveEmployee(string name) {
        TextBlock employeeBlock = new TextBlock() {
            Text = $"{name} je na poslu.",
            Margin = new Avalonia.Thickness(5),
        };
        StackPanel? activeEmployeesList = this.FindControl<StackPanel>("ActiveEmployeesList");
        activeEmployeesList?.Children.Add(employeeBlock);
    }

    public ActiveEmployeesWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        Closing += (sender, e) => {
            MainWindowViewModel.showActiveEmplyeesClosed();
            Logger.log("ActiveEmployeesWindow closed");
        };

        List<String> activeEmployees = db.getActiveEmployees(DateTime.Now);
        if(activeEmployees.Count == 0) {
            MessageBoxManager.GetMessageBoxStandard("Nema radnika", "Trenutno nijedan radnik nije na poslu.", 
            MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            Close();
            Logger.log("No active employees shown");
        }
        else {
            foreach (String employee in activeEmployees) addActiveEmployee(employee);
            Show();
            Logger.log("ActiveEmployeesWindow opened");
        }
    }
}