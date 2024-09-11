using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CheckInOut2.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MsBox.Avalonia;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

partial class EditWorkerWindowViewModel : ObservableObject {
    [ObservableProperty]
    private string _firstName = "";
    [ObservableProperty]
    private string _lastName = "";
    [ObservableProperty]
    private string _chip = "";
    private ObservableCollection<String> _names;
    public ObservableCollection<String> names { 
        get { return _names; }
        private set { _names = value; }
    }
    [ObservableProperty]
    private int _worker = -1;
    private DatabaseInterface db;
    private List<Worker> workers;
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);
    [ObservableProperty]
    private string _hourlyRate = "";
    [ObservableProperty]
    private int _timeConfig = -1; 
    [ObservableProperty]
    private string _salary = "";
    private List<int> timeConfigIDs;
    public ObservableCollection<string> timeConfigs {get; private set;} = new ObservableCollection<string>();

    partial void OnWorkerChanged(int oldValue, int newValue) {
        FirstName = workers[newValue].firstName;
        LastName = workers[newValue].lastName;
        Chip = "Čip: " + workers[newValue].chip;
        HourlyRate = workers[newValue].hourlyRate.ToString();
        TimeConfig = timeConfigIDs.FindIndex(time => time == workers[newValue].timeConfig);
        Salary = workers[newValue].salary.ToString();
        Logger.log($"Worker selected: {names[newValue]}");
    }

    public void saveWorker() {
        string[] chipParts = Chip.Split(' ');
        string error = "Nijedno polje ne može da bude prazno!";
        if(chipParts.Length <= 1 || FirstName.Length == 0 || LastName.Length == 0 || !float.TryParse(HourlyRate, out float hourlyRateVal) || !float.TryParse(Salary, out float salaryVal)
            || TimeConfig < 0 || TimeConfig >= timeConfigs.Count || !db.editWorker(workers[Worker].id, FirstName, LastName, chipParts[1], hourlyRateVal, timeConfigIDs[TimeConfig], salaryVal, ref error)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", error, 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log($"Failed saving({FirstName},{LastName},{Chip}): {error}");
        }
        else {
            Logger.log($"Changed worker from ({workers[Worker].firstName},{workers[Worker].lastName},{workers[Worker].chip}) to ({FirstName},{LastName},{chipParts[1]})");
            MessageBoxManager.GetMessageBoxStandard("Izmenjen", $"Uspešno je izmenjen radnik i sada je {FirstName} {LastName} sa brojem čipa {chipParts[1]}.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            int index = Worker;
            workers[index].firstName = FirstName;
            workers[index].lastName = LastName;
            workers[index].chip = chipParts[1];
            workers[index].hourlyRate = hourlyRateVal;
            workers[index].timeConfig = timeConfigIDs[TimeConfig];
            workers[index].salary = salaryVal;
            _names[index] = FirstName + " " + LastName;
            Worker = index;
        }
    }

    private void updateTimeConfigs() {
        timeConfigs.Clear();
        Models.TimeConfig.ToStrings(db.GetTimeConfigs(), out timeConfigIDs).ForEach(timeConfigs.Add);
    }

    public void addTimeConfig(EditWorkerWindow view) {
        AddTimeConfigWindow addTimeConfigWindow = new AddTimeConfigWindow(updateTimeConfigs, db);
        addTimeConfigWindow.Show(view);
    }

    public EditWorkerWindowViewModel(DatabaseInterface db) {
        this.db = db;
        updateTimeConfigs();
        _names = new ObservableCollection<string>();
        workers = db.getWorkers();
        workers.ForEach(worker => names.Add(worker.firstName + " " + worker.lastName));
    }
}