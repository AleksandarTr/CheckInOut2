using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CheckInOut2.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

partial class AddTimeConfigWindowViewModel : ObservableObject {
    public TimeConfig Monday {get; set;} = new TimeConfig() {day = 0};
    public TimeConfig Tuesday {get; set;} = new TimeConfig() {day = 1};
    public TimeConfig Wednesday {get; set;} = new TimeConfig() {day = 2};
    public TimeConfig Thursday {get; set;} = new TimeConfig() {day = 3};
    public TimeConfig Friday {get; set;} = new TimeConfig() {day = 4};
    public TimeConfig Saturday {get; set;} = new TimeConfig() {day = 5};
    public TimeConfig Sunday {get; set;} = new TimeConfig() {day = 6};
    private TimeConfig[] week;
    private bool[] empty = Enumerable.Repeat(false, 7).ToArray();
    [ObservableProperty]
    private string _fontSize = Settings.get("fontSize")!;
    public ObservableCollection<string> timeConfigs {get; set;} = new ObservableCollection<string>();
    [ObservableProperty]
    private int _timeConfig = 0;
    private DatabaseInterface db;
    [ObservableProperty]
    private string _actionText = "Dodaj";
    private List<int> ids;

    partial void OnTimeConfigChanged(int oldValue, int newValue)
    {
        if(newValue == 0) {
            ActionText = "Dodaj";
            for(int i = 0; i < 7; i++) empty[i] = false;
            foreach(TimeConfig day in week) day.copy(new TimeConfig() {day = day.day});
        }
        else if(newValue > 0 && newValue < timeConfigs.Count) {
            ActionText = "Sačuvaj";
            foreach(TimeConfig day in week) {
                TimeConfig? timeConfig = db.GetTimeConfig(ids[newValue - 1], day.day);
                day.copy(timeConfig ?? new TimeConfig() {day = day.day, id = ids[newValue - 1]});
                empty[day.day] = timeConfig == null;
            }
        }
        else ActionText = "Izaberite radno vreme";
    }

    public void actionClick() {
        if(TimeConfig < 0 || TimeConfig >= timeConfigs.Count) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Niste izabrali radno vreme ili opciju za dodavanje novog radnog vremena.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        bool[] emptyAfter = Enumerable.Repeat(false, 7).ToArray();
        foreach (TimeConfig day in week) {
            if(day.HourStart.Length == 0 && day.HourEnd.Length == 0 && day.MinuteStart.Length == 0 && day.MinuteEnd.Length == 0) {
                emptyAfter[day.day] = true;
                continue;
            }

            if(day.HourStart.Length == 0 || day.HourEnd.Length == 0 || day.MinuteStart.Length == 0 || day.MinuteEnd.Length == 0) {
                MessageBoxManager.GetMessageBoxStandard("Greška", "Ili sva ili nijedno polje za jedan radni dan moraju da budu popunjena.",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }

            if(!int.TryParse(day.HourStart, out int hourStart) || !int.TryParse(day.MinuteStart, out int minuteStart) || !int.TryParse(day.HourEnd, out int hourEnd) || !int.TryParse(day.MinuteEnd, out int minuteEnd) ||
                hourStart < 0 || hourStart > hourEnd || minuteStart < 0 || minuteStart > 59 || (minuteStart > minuteEnd && hourStart == hourEnd) ||
                hourEnd < 0 || hourEnd > 23 || minuteEnd < 0 || minuteEnd > 59) {
                MessageBoxManager.GetMessageBoxStandard("Greška", "U polju mora da bude uneseno validno vreme i kraj smene mora da bude nakon početka.",
                    MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                return;
            }
        }

        List<TimeConfig> nonEmpty = new List<TimeConfig>();
        if(TimeConfig == 0) {
            int newId = db.getNextTimeConfigID();
            foreach (TimeConfig day in week) {
                day.id = newId;
                if(!emptyAfter[day.day]) {
                    db.addTimeConfig(day);
                    nonEmpty.Add(day);
                }
            }

            MessageBoxManager.GetMessageBoxStandard("Dodato", "Uspešno je dodato novo radno vreme.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();

            timeConfigs.Add(Models.TimeConfig.ToStrings(nonEmpty, out _)[0]);
            ids.Add(newId);
            return;
        }

        foreach (TimeConfig day in week) {
            if(empty[day.day] && !emptyAfter[day.day]) db.addTimeConfig(day);
            else if(!empty[day.day] && emptyAfter[day.day]) db.deleteTimeConfig(day);
            else if(!empty[day.day] && !emptyAfter[day.day]) db.editTimeConfig(day);
            if(!emptyAfter[day.day]) nonEmpty.Add(day);
        }

        MessageBoxManager.GetMessageBoxStandard("Izmenjeno", "Uspešno je izmenjeno radno vreme.", 
            MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();

        timeConfigs[TimeConfig] = Models.TimeConfig.ToStrings(nonEmpty, out _)[0];
    }

    public AddTimeConfigWindowViewModel(DatabaseInterface db) {
        this.db = db;
        timeConfigs.Add("");
        Models.TimeConfig.ToStrings(db.GetTimeConfigs(), out ids).ForEach(timeConfigs.Add);
        week = [Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday];
    }
}