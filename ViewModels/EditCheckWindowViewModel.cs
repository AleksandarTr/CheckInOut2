using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

partial class EditCheckWindowViewModel : ObservableObject {
    [ObservableProperty]
    private string _day = DateTime.Now.Day.ToString("00");
    [ObservableProperty]
    private string _month = DateTime.Now.Month.ToString("00");
    [ObservableProperty]
    private string _year = DateTime.Now.Year.ToString("");
    [ObservableProperty]
    private string _hour = "";
    [ObservableProperty]
    private string _minute = "";

    private ObservableCollection<String> _checks;
    public ObservableCollection<String> checks { 
        get { return _checks; }
        private set { _checks = value; }
    }

    [ObservableProperty]
    private int _check = -1;
    [ObservableProperty]
    private string _name = "";

    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);

    private DatabaseInterface db;
    private List<Check> checkInfo = new List<Check>();

    partial void OnDayChanged(string value)
    {
        onDateChanged();
    }

    partial void OnMonthChanged(string value)
    {
        onDateChanged();
    }

    partial void OnYearChanged(string value)
    {
        onDateChanged();
    }

    private void onDateChanged() {
        checks.Clear();
        Check = -1;
        int day, month, year;
        if(int.TryParse(Day, out day) && int.TryParse(Month, out month) && int.TryParse(Year, out year)){
            if(day < 1 || day > 31) return;
            if(month < 1 || month > 12) return;
            if(year < 2000) return;
            checkInfo = db.getChecks(new DateTime(year, month, day));
            checkInfo.ForEach(check => checks.Add($"{check.time:HH:mm}-{check.worker.firstName.First()}{check.worker.lastName.First()}"));
            Logger.log($"Loaded checks for {year}.{month}.{day}");
        }
    }

    partial void OnCheckChanged(int value) {
        if(value < 0 || value >= checks.Count) return;
        Name = checkInfo[value].worker.firstName + " " + checkInfo[value].worker.lastName;
        Hour = checkInfo[value].time.Hour.ToString();
        Minute = checkInfo[value].time.Minute.ToString();
        Logger.log($"Selected check: {checks[value]}");
    }

    public void saveCheck() {
        if(Check < 0 || Check >= checks.Count) {
            Logger.log("Failed saving: no check selected");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije izabrano čekiranje.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(!int.TryParse(Day, out int day) || !int.TryParse(Month, out int month) || !int.TryParse(Year, out int year) || !int.TryParse(Hour, out int hour) || !int.TryParse(Minute, out int minute)) {
            Logger.log("Failed saving: no date/time entered");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije unesen pravilan datum i vreme", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(hour < 0 || hour > 23 || minute < 0 || minute > 59) {
            Logger.log("Failed saving: invalid time");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Uneseno je nekorektno vreme", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(db.editCheck(checkInfo[Check].id, new DateTime(year, month, day, hour, minute, 0))) {
            Logger.log($"Saved check for {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName} from {checkInfo[Check].time:HH:mm} to {hour}:{minute} {day}.{month}.{year}");
            MessageBoxManager.GetMessageBoxStandard("Promenjeno", 
                $"Uspešno je promenjeno čekiranje za {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName} na {hour}:{minute} {day}.{month}.{year}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            checkInfo[Check].time = new DateTime(year, month, day, hour, minute, 0);
            checks[Check] = $"{hour:00}:{minute:00}-{checkInfo[Check].worker.firstName.First()}{checkInfo[Check].worker.lastName.First()}";
        }
        else {
            MessageBoxManager.GetMessageBoxStandard("Greška", 
                $"Nije moglo da se promeni vreme čekiranja za {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log("Failed saving: unknown error");
        }
    }

    public void deleteCheck(){
        if(Check < 0 || Check >= checks.Count) {
            Logger.log("Failed deleting: No check selected");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije izabrano čekiranje.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(db.deleteCheck(checkInfo[Check].id)) {
            Logger.log($"Deleted check for {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName} at {checkInfo[Check].time:yyyy.MM.dd-HH:mm}");
            MessageBoxManager.GetMessageBoxStandard("Promenjeno", 
                $"Uspešno je izbrisano čekiranje za {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            checkInfo.RemoveAt(Check);
            checks.RemoveAt(Check);
        }
        else {
            Logger.log("Failed deleting: unknonw error");
            MessageBoxManager.GetMessageBoxStandard("Greška", 
                $"Nije moglo da se izbriše čekiranje za {checkInfo[Check].worker.firstName} {checkInfo[Check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }

    public EditCheckWindowViewModel(DatabaseInterface db, EditCheckWindow view) {
        this.db = db;
        _checks = new ObservableCollection<string>();
        onDateChanged();
    }
}