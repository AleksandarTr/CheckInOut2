using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CheckInOut2.Models;
using CheckInOut2.Views;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class EditCheckWindowViewModel : INotifyPropertyChanged {
    private string _day = DateTime.Now.Day.ToString("00");
    public string day { 
        get { return _day; } 
        set { 
            _day = value;
            onDateChanged();
        } }
    private string _month = DateTime.Now.Month.ToString("00");
    public string month { 
        get { return _month; } 
        set { 
            _month = value;
            onDateChanged();
        } }
    private string _year = DateTime.Now.Year.ToString("");
    public string year { 
        get { return _year; } 
        set { 
            _year = value;
            onDateChanged();
        } }
    private string _hour = "";
    public string hour {
        get { return _hour; }
        set {
            _hour = value;
            OnPropertyChanged();
        }
    }
    private string _minute = "";
    public string minute {
        get { return _minute; }
        set {
            _minute = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<String> _checks;
    public ObservableCollection<String> checks { 
        get { return _checks; }
        private set { _checks = value; }
    }

    private int _check = -1;
    public int check {
        get { return _check; }
        set {
            _check = value;
            onChecKSelected();
        }
    }
    private string _name = "";
    public string name {
        get => _name;
        set {
            _name = value;
            OnPropertyChanged();
        }
    }

    private DatabaseInterface db;
    private List<Check> checkInfo = new List<Check>();

    private void onDateChanged() {
        checks.Clear();
        _check = -1;
        int day, month, year;
        if(int.TryParse(this.day, out day) && int.TryParse(this.month, out month) && int.TryParse(this.year, out year)){
            if(day < 1 || day > 31) return;
            if(month < 1 || month > 12) return;
            if(year < 2000) return;
            checkInfo = db.getChecks(new DateTime(year, month, day));
            checkInfo.ForEach(check => checks.Add($"{check.time.ToString("HH:mm")}-{check.worker.firstName.First()}{check.worker.lastName.First()}"));
        }
    }

    private void onChecKSelected() {
        if(check < 0 || check >= checks.Count) return;
        name = checkInfo[check].worker.firstName + " " + checkInfo[check].worker.lastName;
        hour = checkInfo[check].time.Hour.ToString();
        minute = checkInfo[check].time.Minute.ToString();
    }

    public void saveCheck() {
        if(check < 0 || check >= checks.Count) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije izabrano čekiranje.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(!int.TryParse(this.day, out int day) || !int.TryParse(this.month, out int month) || !int.TryParse(this.year, out int year) || !int.TryParse(this.hour, out int hour) || !int.TryParse(this.minute, out int minute)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije unesen pravilan datum i vreme", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(hour < 0 || hour > 23 || minute < 0 || minute > 59) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Uneseno je nekorektno vreme", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(db.editCheck(checkInfo[check].id, new DateTime(year, month, day, hour, minute, 0))) {
            MessageBoxManager.GetMessageBoxStandard("Promenjeno", 
                $"Uspešno je promenjeno čekiranje za {checkInfo[check].worker.firstName} {checkInfo[check].worker.lastName} na {hour}:{minute} {day}.{month}.{year}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            checkInfo[check].time = new DateTime(year, month, day, hour, minute, 0);
            checks[check] = $"{hour:00}:{minute:00}-{checkInfo[check].worker.firstName.First()}{checkInfo[check].worker.lastName.First()}";
        }
        else MessageBoxManager.GetMessageBoxStandard("Greška", 
                $"Nije moglo da se promeni vreme čekiranja za {checkInfo[check].worker.firstName} {checkInfo[check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
    }

    public void deleteCheck(){
        if(check < 0 || check >= checks.Count) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nije izabrano čekiranje.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        if(db.deleteCheck(checkInfo[check].id)) {
            MessageBoxManager.GetMessageBoxStandard("Promenjeno", 
                $"Uspešno je izbrisano čekiranje za {checkInfo[check].worker.firstName} {checkInfo[check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            checkInfo.RemoveAt(check);
            checks.RemoveAt(check);
        }
        else MessageBoxManager.GetMessageBoxStandard("Greška", 
                $"Nije moglo da se izbriše čekiranje za {checkInfo[check].worker.firstName} {checkInfo[check].worker.lastName}.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public EditCheckWindowViewModel(DatabaseInterface db, EditCheckWindow view) {
        this.db = db;
        _checks = new ObservableCollection<string>();
        onDateChanged();
    }
}