using CheckInOut2.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

partial class AddTimeConfigWindowViewModel : ObservableObject {
    public TimeConfig Monday {get; set;} = new TimeConfig() {day = 0};
    public TimeConfig Tuesday {get; set;} = new TimeConfig() {day = 1};
    public TimeConfig Wednesday {get; set;} = new TimeConfig() {day = 2};
    public TimeConfig Thursday {get; set;} = new TimeConfig() {day = 3};
    public TimeConfig Friday {get; set;} = new TimeConfig() {day = 4};
    public TimeConfig Saturday {get; set;} = new TimeConfig() {day = 5};
    public TimeConfig Sunday {get; set;} = new TimeConfig() {day = 6};
    [ObservableProperty]
    private string _fontSize = Settings.get("fontSize")!;
}