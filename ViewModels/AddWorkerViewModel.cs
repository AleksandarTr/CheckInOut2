using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

partial class AddWorkerWindowViewModel : ObservableObject {
    public string firstName {get;set;} = "";
    public string lastName {get;set;} = "";
    public string chip {get;set;} = "Čip: ";
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);
    public string hourlyRate {get;set;} = "";
    public int timeConfig {get; set;} = -1;
    public ObservableCollection<string> timeConfigs {get; set;} = new ObservableCollection<string>();
    private List<int> timeConfigIDs;
    private DatabaseInterface db;

    public void addWorker() {
        string[] chipParts = chip.Split(' ');
        string error = "Nijedno polje ne može da bude prazno!";

        if(chipParts.Length <= 1 || firstName.Length == 0 || lastName.Length == 0 || !float.TryParse(hourlyRate, out float hourlyRateVal)
            || timeConfig < 0 || timeConfig >= timeConfigs.Count || !db.addWorker(firstName, lastName, chipParts[1], hourlyRateVal, timeConfigIDs[timeConfig], ref error)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", error, 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log($"Failed to add worker({firstName},{lastName},{chipParts[1]}):{error}");
        }
        else {
            MessageBoxManager.GetMessageBoxStandard("Dodato", $"Uspešno je dodat radnik {firstName} {lastName} sa brojem čipa {chipParts[1]}.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            Logger.log($"Added worker: {firstName},{lastName},{chipParts[1]}):{error}");
        }
    }

    private void updateTimeConfigs() {
        timeConfigs.Clear();
        TimeConfig.ToStrings(db.GetTimeConfigs(), out timeConfigIDs).ForEach(timeConfigs.Add);
    }

    public void addTimeConfig(AddWorkerWindow view) {
        AddTimeConfigWindow addTimeConfigWindow = new AddTimeConfigWindow(updateTimeConfigs, db);
        addTimeConfigWindow.Show(view);
    }

    public AddWorkerWindowViewModel(DatabaseInterface db) {
        this.db = db;
        updateTimeConfigs();
    }
}