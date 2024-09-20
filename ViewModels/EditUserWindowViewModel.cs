using System.Collections.Generic;
using System.Collections.ObjectModel;
using CheckInOut2.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

partial class EditUserWindowViewModel : ObservableObject { 
    [ObservableProperty]
    private string _username = "";
    public string password { get; set; } = "";
    [ObservableProperty]
    private string _chip = "Čip: ";
    [ObservableProperty]
    private bool _addWorker = false;
    [ObservableProperty]
    private bool _checkWorker = false;
    [ObservableProperty]
    private bool _editWorker = false;
    [ObservableProperty]
    private bool _export = false;
    [ObservableProperty]
    private bool _editCheck = false;
    [ObservableProperty]
    private bool _addUser = false;
    [ObservableProperty]
    private bool _editUser = false;
    [ObservableProperty]
    private bool _close = false;
    [ObservableProperty]
    private bool _settings = false;

    public int fontSize {get; set;} = int.Parse(Models.Settings.get("fontSize")!);
    
    private ObservableCollection<string> _users;
    public ObservableCollection<string> users {
        get { return _users; }
        private set { _users = value; }
    }
    [ObservableProperty]
    private int _user = -1;
    private DatabaseInterface db;
    private List<User> userList = new List<User>();

    partial void OnUserChanged(int value) {
        if(value < 0 || value > users.Count) return;
        Username = userList[value].username;
        Chip = $"Čip: {userList[value].chip}";
        AddWorker = (userList[value].permission & 1) != 0;
        CheckWorker = (userList[value].permission & 2) != 0;
        EditWorker = (userList[value].permission & 4) != 0;
        Export = (userList[value].permission & 8) != 0;
        EditCheck = (userList[value].permission & 16) != 0;
        AddUser = (userList[value].permission & 32) != 0;
        EditUser = (userList[value].permission & 64) != 0;
        Close = (userList[value].permission & 128) != 0;
        Settings = (userList[value].permission & 256) != 0;
        Logger.log($"Selected user {users[value]}");
    }

    public void saveUser() {
        string error = "";
        string[] chipParts = Chip.Split(" ");
        if(Username.Length == 0 || password.Length == 0 || chipParts.Length < 2) error = "Nijedno polje ne može da bude prazno.";
        if(User < 0 || User >= users.Count) error = "Nije izabran korisnik.";

        int permission = AddWorker ? 1 : 0;
        permission |= CheckWorker ? 2 : 0;
        permission |= EditWorker ? 4 : 0;
        permission |= Export ? 8 : 0;
        permission |= EditCheck ? 16 : 0;
        permission |= AddUser ? 32 : 0;
        permission |= EditUser ? 64 : 0;
        permission |= Close ? 128 : 0;
        permission |= Settings ? 256 : 0;
        string hashedPassword = LogInWindowViewModel.ComputeSha256Hash(password);

        if(error.Length == 0) db.editUser(userList[User].username, Username, hashedPassword, chipParts[1], permission, ref error);
        if(error.Length != 0){
            MessageBoxManager.GetMessageBoxStandard("Greška", error, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log($"Failed saving: {error}");
        }
        else {
            Logger.log($"Changed user from ({userList[User].username},#####,{userList[User].chip},{userList[User].permission}) to ({Username},{hashedPassword},{chipParts[1]},{permission})");
            MessageBoxManager.GetMessageBoxStandard("Promenjen korisnik", "Uspešno je izmenjen korisnik", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            userList[User].username = Username;
            userList[User].chip = chipParts[1];
            userList[User].permission = permission;
            users[User] = Username;
        }
    }
    
    public EditUserWindowViewModel(DatabaseInterface db){
        this.db = db;
        _users = new ObservableCollection<string>();
        userList = db.getUsers();
        userList.ForEach(user => users.Add(user.username));
    }
}