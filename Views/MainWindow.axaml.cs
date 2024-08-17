using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CheckInOut2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        StackPanel? informationBoard = this.FindControl<StackPanel>("informationBoard");
    }
}