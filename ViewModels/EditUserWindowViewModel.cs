using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CheckInOut2.Models;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class EditUserWindowViewModel : INotifyPropertyChanged { 
    private string _username = "";
    public string username { 
        get { return _username; }
        set { 
            _username = value;
            OnPropertyChanged();
        }}
    public string password { get; set; } = "";
    private string _chip = "Čip: ";
    public string chip { 
        get { return _chip; }
        set { 
            _chip = value;
            OnPropertyChanged();
        }}
    private bool _addWorker = false;
    public bool addWorker { 
        get { return _addWorker; }
        set { 
            _addWorker = value;
            OnPropertyChanged();
        }}
    private bool _checkWorker = false;
    public bool checkWorker { 
        get { return _checkWorker; }
        set { 
            _checkWorker = value;
            OnPropertyChanged();
        }}
    private bool _editWorker = false;
    public bool editWorker { 
        get { return _editWorker; }
        set { 
            _editWorker = value;
            OnPropertyChanged();
        }}
    private bool _export = false;
    public bool export { 
        get { return _export; }
        set { 
            _export = value;
            OnPropertyChanged();
        }}
    private bool _editCheck = false;
    public bool editCheck { 
        get { return _editCheck; }
        set { 
            _editCheck = value;
            OnPropertyChanged();
        }}
    private bool _addUser = false;
    public bool addUser { 
        get { return _addUser; }
        set { 
            _addUser = value;
            OnPropertyChanged();
        }}
    private bool _editUser = false;
    public bool editUser { 
        get { return _editUser; }
        set { 
            _editUser = value;
            OnPropertyChanged();
        }}
    private bool _close = false;
    public bool close { 
        get { return _close; }
        set { 
            _close = value;
            OnPropertyChanged();
        }}
    private ObservableCollection<string> _users;
    public ObservableCollection<string> users {
        get { return _users; }
        private set { _users = value; }
    }
    private int _user = -1;
    public int user {
        get { return _user; }
        set {
            _user = value;
            onUserChanged();
        }
    }
    private DatabaseInterface db;
    private List<User> userList = new List<User>();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void onUserChanged() {
        username = userList[user].username;
        chip = $"Čip: {userList[user].chip}";
        addWorker = (userList[user].permission & 1) != 0;
        checkWorker = (userList[user].permission & 2) != 0;
        editWorker = (userList[user].permission & 4) != 0;
        export = (userList[user].permission & 8) != 0;
        editCheck = (userList[user].permission & 16) != 0;
        addUser = (userList[user].permission & 32) != 0;
        editUser = (userList[user].permission & 64) != 0;
        close = (userList[user].permission & 128) != 0;
    }

    public void saveUser() {
        string error = "";
        string[] chipParts = chip.Split(" ");
        if(username.Length == 0 || password.Length == 0 || chipParts.Length < 2) error = "Nijedno polje ne može da bude prazno.";
        if(user < 0 || user >= users.Count) error = "Nije izabran korisnik.";

        int permission = addWorker ? 1 : 0;
        permission |= checkWorker ? 2 : 0;
        permission |= editWorker ? 4 : 0;
        permission |= export ? 8 : 0;
        permission |= editCheck ? 16 : 0;
        permission |= addUser ? 32 : 0;
        permission |= editUser ? 64 : 0;
        permission |= close ? 128 : 0;
        string hashedPassword = LogInWindowViewModel.ComputeSha256Hash(password);

        if(error.Length == 0) db.editUser(userList[user].username, username, hashedPassword, chipParts[1], permission, ref error);
        if(error.Length != 0)
            MessageBoxManager.GetMessageBoxStandard("Greška", error, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        else {
            MessageBoxManager.GetMessageBoxStandard("Promenjen korisnik", "Uspešno je izmenjen korisnik", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            userList[user].username = username;
            userList[user].chip = chipParts[1];
            userList[user].permission = permission;
            users[user] = username;
        }
    }
    
    public EditUserWindowViewModel(DatabaseInterface db){
        this.db = db;
        _users = new ObservableCollection<string>();
        userList = db.getUsers();
        userList.ForEach(user => users.Add(user.username));
    }
}