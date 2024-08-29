using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.Models;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class WorkerCheckWindow : Window {
    public WorkerCheckWindow(DatabaseInterface db) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new WorkerCheckWindowViewModel(db);
    }
}