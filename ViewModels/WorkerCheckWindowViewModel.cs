using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CheckInOut2.Models;
using CheckInOut2.Views;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class WorkerCheckWindowViewModel
{
    public string day { get; set; } = DateTime.Now.Day.ToString("00");
    public string month { get; set; } = DateTime.Now.Month.ToString("00");
    public string year { get; set; } = DateTime.Now.Year.ToString();
    public string hour { get; set; } = DateTime.Now.Hour.ToString("00");
    public string minute { get; set; } = DateTime.Now.Minute.ToString("00");
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);

    private ObservableCollection<String> _names;
    public ObservableCollection<String> names { 
        get { return _names; }
        private set { _names = value; }
     }

    public int worker {get; set; } = -1;

    private DatabaseInterface db;
    private List<Worker> workers;

    public void checkWorker() {
        string error = "";
        if(worker < 0 || worker >= names.Count) error = "Nije izabran radnik.";
        else if(day.Length == 0 || month.Length == 0 || year.Length == 0 || minute.Length == 0 || hour.Length == 0) error = "Nijedno polje ne može da bude prazno.";  
        
        if(error.Length != 0) {
            MessageBoxManager.GetMessageBoxStandard("Greška", error, 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log($"Failed checking worker: {error}");
        }
        else {
            Logger.log($"Manuel check attempt: {workers[worker].firstName} {workers[worker].lastName} at {year}.{month}.{day}-{hour}:{minute}");
            string message = db.logCheckIn(workers[worker].chip, new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), int.Parse(minute), 0));
            MessageBoxManager.GetMessageBoxStandard("Čekiranje", message, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            MainWindow.addMessage(message);
        }
    }

    public WorkerCheckWindowViewModel(DatabaseInterface db) {
        workers = db.getWorkers();
        _names = new ObservableCollection<string>();
        workers.ForEach(worker => _names.Add(worker.firstName + " " + worker.lastName));
        this.db = db;
    }
}