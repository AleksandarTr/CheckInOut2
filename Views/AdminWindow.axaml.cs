using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Views;

partial class AdminWindow : Window {
    public AdminWindow(int permission) {
        AvaloniaXamlLoader.Load(this);
        DataContext = new AdminWindowViewModel();
        SizeToContent = SizeToContent.WidthAndHeight;

        if((permission & 1) == 0) this.FindControl<Button>("addWorker")!.IsEnabled = false;
        if((permission & 2) == 0) this.FindControl<Button>("exportActivity")!.IsEnabled = false;
        if((permission & 4) == 0) this.FindControl<Button>("checkWorker")!.IsEnabled = false;
        if((permission & 8) == 0) this.FindControl<Button>("editWorker")!.IsEnabled = false;
        if((permission & 16) == 0) this.FindControl<Button>("editCheck")!.IsEnabled = false;
        if((permission & 32) == 0) this.FindControl<Button>("closeProgram")!.IsEnabled = false;
    }
}