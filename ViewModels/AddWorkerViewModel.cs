using System;
using CheckInOut2.Models;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class AddWorkerWindowViewModel {
    public string firstName {get;set;} = "";
    public string lastName {get;set;} = "";
    public string chip {get;set;} = "Čip: ";
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);

    private DatabaseInterface db;

    public void addWorker() {
        string[] chipParts = chip.Split(' ');
        string error = "Nijedno polje ne može da bude prazno!";
        if(chipParts.Length <= 1 || firstName.Length == 0 || lastName.Length == 0 || !db.addWorker(firstName, lastName, chipParts[1], ref error)) {
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

    public AddWorkerWindowViewModel(DatabaseInterface db) {
        this.db = db;
    }
}