using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CheckInOut2.Models;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

public enum ExportPeriod { Day, Month, Year }

class ExportWindowViewModel { 
    public string day { get; set; } = DateTime.Now.Day.ToString("00");
    public string month { get; set; } = DateTime.Now.Month.ToString("00");
    public string year { get; set; } = DateTime.Now.Year.ToString("0000");
    private DatabaseInterface db;

    private ObservableCollection<String> _formats;
    public ObservableCollection<String> formats { 
        get { return _formats; }
        private set { _formats = value; }
    }

    public int format {get; set; } = 0;

    private void exportCSV(List<Check> checks, string fileName) {
        if(!Directory.Exists("izvestaji")) Directory.CreateDirectory("izvestaji");
        StreamWriter log = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}.csv");

        DateTime date = DateTime.MinValue;
        List<Check> unmatched = new List<Check>();
        bool first = true;
        log.WriteLine("Ime i prezime,Dolazak,Odlazak,Datum");

        foreach (Check check in checks) {
            if (check.time.Date != date.Date) {
                foreach (Check unmatchedCheck in unmatched) 
                    log.WriteLine($"{unmatchedCheck.worker.firstName} {unmatchedCheck.worker.lastName},{unmatchedCheck.time:HH:mm},,{unmatchedCheck.time:dd.MM.yyyy}");
                date = check.time;
                first = false;
                unmatched.Clear();
            }
            
            Check? match = unmatched.Find(unmatchedCheck => unmatchedCheck.worker.id == check.worker.id);
            if(match == null) unmatched.Add(check);
            else {
                log.WriteLine($"{match.worker.firstName} {match.worker.lastName},{match.time:HH:mm},{check.time:HH:mm},{check.time:dd.MM.yyyy}");
                unmatched.Remove(match);
            }
        }

        foreach (Check unmatchedCheck in unmatched) 
                    log.WriteLine($"{unmatchedCheck.worker.firstName} {unmatchedCheck.worker.lastName},{unmatchedCheck.time:HH:mm},,{unmatchedCheck.time:dd.MM.yyyy}");

        log.Close();
    }

    private void exportTXT(List<Check> checks, string fileName) {
        if(!Directory.Exists("izvestaji")) Directory.CreateDirectory("izvestaji");
        StreamWriter log = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}.txt");

        DateTime date = DateTime.MinValue;
        List<Check> unmatched = new List<Check>();
        bool first = true;
        foreach (Check check in checks) {
            if (check.time.Date != date.Date) {
                foreach (Check unmatchedCheck in unmatched) 
                    log.WriteLine($"{unmatchedCheck.worker.firstName} {unmatchedCheck.worker.lastName} {unmatchedCheck.time:HH:mm}-...");
                date = check.time;
                log.Write($"{(first ? "" : "\n")}{check.time:dd.MM.yyyy}\n");
                first = false;
                unmatched.Clear();
            }
            
            Check? match = unmatched.Find(unmatchedCheck => unmatchedCheck.worker.id == check.worker.id);
            if(match == null) unmatched.Add(check);
            else {
                log.WriteLine($"{match.worker.firstName} {match.worker.lastName} {match.time:HH:mm}-{check.time:HH:mm}");
                unmatched.Remove(match);
            }
        }

        foreach (Check unmatchedCheck in unmatched) 
                    log.WriteLine($"{unmatchedCheck.worker.firstName} {unmatchedCheck.worker.lastName} {unmatchedCheck.time:HH:mm}-...");

        log.Close();
    }

    public void export() {
        if(format < 0 || format > _formats.Count) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Niste izabrali korektan format.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        int _day = 1, _month = 1, _year = 2000;
        if((day.Length != 0 && (!int.TryParse(day, out _day) || _day < 1 || _day > 31)) || 
           (month.Length != 0 && (!int.TryParse(month, out _month) || _month < 1 || _month > 12)) || 
           !int.TryParse(year, out _year) || _year < 2000) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Niste uneli pravilan datum!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        ExportPeriod period;
        if (month.Length == 0 && day.Length == 0) period = ExportPeriod.Year;
        else if (day.Length == 0) period = ExportPeriod.Month;
        else if (month.Length == 0) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Polje za mesec ne može da ostane prazno, a da je popunjeno polje za dan.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        else period = ExportPeriod.Day;

        string fileName = period switch {
            ExportPeriod.Year => $"Log-{_year:0000}",
            ExportPeriod.Month => $"Log-{_year:0000}-{_month:00}",
            _ => $"Log-{_year:0000}-{_month:00}-{_day:00}",
        };

        List<Check> checks = db.getChecks(new DateTime(_year, _month, _day), period);
        switch(format) {
            case 0: exportTXT(checks, fileName); break;
            case 1: exportCSV(checks, fileName); break;
            default: throw new ArgumentException("Invalid format selected.");
        };

        MessageBoxManager.GetMessageBoxStandard("Izvezeno", "Uspešno je napravljen isveštaj.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
    }

    public ExportWindowViewModel(DatabaseInterface db) {
        this.db = db;
        _formats = new ObservableCollection<String>() {"txt", "csv"};
    }
}