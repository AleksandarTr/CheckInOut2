using CheckInOut2.Models;
using CheckInOut2.Views;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class AddUserWindowViewModel{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
    public string chip { get; set; } = "Čip: ";
    public bool addWorker { get; set; } = false;
    public bool checkWorker { get; set; } = false;
    public bool editWorker { get; set; } = false;
    public bool export { get; set; } = false;
    public bool editCheck { get; set; } = false;
    public bool addUser { get; set; } = false;
    public bool editUser { get; set; } = false;
    public bool close { get; set; } = false;
    public bool settings { get; set; } = false;
    private DatabaseInterface db;

    public void onAddUser() {
        int permission = addWorker ? 1 : 0;
        permission |= checkWorker ? 2 : 0;
        permission |= editWorker ? 4 : 0;
        permission |= export ? 8 : 0;
        permission |= editCheck ? 16 : 0;
        permission |= addUser ? 32 : 0;
        permission |= editUser ? 64 : 0;
        permission |= close ? 128 : 0;
        permission |= settings ? 256 : 0;

        string[] chipParts = chip.Split(" ");
        if(username.Length == 0 || password.Length == 0 || chipParts.Length < 2) {
            Logger.log("Failed to add user becuase of empty field.");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nijedno polje ne može da bude prazno.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        string hashedPassword = LogInWindowViewModel.ComputeSha256Hash(password);

        string error = "";
        if(!db.addUser(username, hashedPassword, chipParts[1], permission, ref error)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", error, MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            Logger.log($"Failed to add user({username},{hashedPassword},{chipParts[1]},{permission}): {error}");
        }
        else {
            MessageBoxManager.GetMessageBoxStandard("Dodat korisnik", $"Uspešno je dodat radnik sa korisničkim imenom {username} i čipom {chipParts[1]}", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            Logger.log($"Added user: {username},{hashedPassword},{chipParts[1]},{permission}");
        }
    }

    public AddUserWindowViewModel(DatabaseInterface db) {
        this.db = db;
    }
}