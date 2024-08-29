using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using CheckInOut2.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class EditWorkerWindowViewModel : INotifyPropertyChanged {
    private string _firstName = "";

    public string firstName
    {
        get => _firstName;
        set
        {
            if (_firstName != value)
            {
                _firstName = value;
                OnPropertyChanged();
            }
        }
    }
    private string _lastName = "";
    public string lastName {
        get => _lastName;
        set
        {
            if (_lastName != value)
            {
                _lastName = value;
                OnPropertyChanged();
            }
        }
    }
    private string _chip = "";
    public string chip {
        get => _chip;
        set
        {
            if (_chip != value)
            {
                _chip = value;
                OnPropertyChanged();
            }
        }
    }
    private ObservableCollection<String> _names;
    public ObservableCollection<String> names { 
        get { return _names; }
        private set { _names = value; }
     }

    private int _worker = -1;
    public int worker {get {return _worker;}
     set {
        _worker = value;
        if(value >= 0 && value < _names.Count) workerSelected();
        }}
    private DatabaseInterface db;
    private List<Worker> workers;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void workerSelected() {
        firstName = workers[worker].firstName;
        lastName = workers[worker].lastName;
        chip = "Čip: " + workers[worker].chip;
    }

    public void saveWorker() {
        string[] chipParts = chip.Split(' ');
        string error = "Nijedno polje ne može da bude prazno!";
        if(chipParts.Length <= 1 || firstName.Length == 0 || lastName.Length == 0 || !db.editWorker(workers[worker].id, firstName, lastName, chipParts[1], ref error)) 
            MessageBoxManager.GetMessageBoxStandard("Greška", error, 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        else {
            MessageBoxManager.GetMessageBoxStandard("Izmenjen", $"Uspešno je izmenjen radnik i sada je {firstName} {lastName} sa brojem čipa {chipParts[1]}.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            int index = worker;
            workers[index].firstName = firstName;
            workers[index].lastName = lastName;
            workers[index].chip = chipParts[1];
            _names[index] = firstName + " " + lastName;
            worker = index;

        }
    }

    public EditWorkerWindowViewModel(DatabaseInterface db) {
        this.db = db;
        _names = new ObservableCollection<string>();
        workers = db.getWorkers();
        workers.ForEach(worker => names.Add(worker.firstName + " " + worker.lastName));
    }
}