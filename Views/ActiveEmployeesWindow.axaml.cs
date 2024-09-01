using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

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

    protected override void OnClosing(WindowClosingEventArgs e) {

        base.OnClosing(e);
    }

    public ActiveEmployeesWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        Closing += (sender, e) => MainWindowViewModel.showActiveEmplyeesClosed();
        DataContext = new ActiveEmployeesWindowViewModel(db, this);
    }
}