using System;
using System.Security.Cryptography;
using System.Text;
using CheckInOut2.Models;
using CheckInOut2.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

class LogInWindowViewModel : ObservableObject {
    public string username {get; set;} = "";
    public string password {get; set;} = "";
    public string chip {get; set;} = "";

    public static int MAX_PERMISSION = 63;

    private DatabaseInterface db;
    private LogInWindow view;

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

    public LogInWindowViewModel(DatabaseInterface db, LogInWindow view) {
        this.db = db;
        this.view = view;
    }

    public void logIn() {
        string[] chipParts = chip.Split(' ');
        string user = this.username;
        string password = this.password;
        int permission = db.checkCertification(username, ComputeSha256Hash(password), chipParts.Length > 1 ? chipParts[1] : "");
        if(permission == 0) return;

        AdminWindow adminWindow = new AdminWindow(permission);
        adminWindow.Show();
        view.Close();  
    }
}