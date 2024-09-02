using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using CheckInOut2.Models;
using CheckInOut2.Views;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class LogInWindowViewModel {
    public string username {get; set;} = "";
    public string password {get; set;} = "";
    public string chip {get; set;} = "Čip: ";

    public static int MAX_PERMISSION = 255;

    private DatabaseInterface db;

    public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

    public LogInWindowViewModel(DatabaseInterface db) {
        this.db = db;
    }

    public void logIn(Window view) {
        string[] chipParts = chip.Split(' ');
        int permission = db.checkCertification(username, ComputeSha256Hash(password), chipParts.Length > 1 ? chipParts[1] : "");

        if(permission == 0) {
            Logger.log($"Failed login: {username},####,{chip}");
            MessageBoxManager.GetMessageBoxStandard("Neuspešna prijava", 
                "Ne postoji korisnik sa datom kombinacijom šifre i korisničkog imena.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        Logger.log($"Logged in as {username}");
        AdminWindow adminWindow = new AdminWindow(permission, db);
        adminWindow.Show((view.Owner as Window)!);
        view.Close();  
    }
}