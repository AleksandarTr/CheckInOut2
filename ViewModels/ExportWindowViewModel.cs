using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using CheckInOut2.Models;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

public enum ExportPeriod { Day, Month, Year }

class ExportWindowViewModel { 
    public string day { get; set; } = DateTime.Now.Day.ToString("00");
    public string month { get; set; } = DateTime.Now.Month.ToString("00");
    public string year { get; set; } = DateTime.Now.Year.ToString("0000");
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);
    private DatabaseInterface db;

    private ObservableCollection<String> _formats;
    public ObservableCollection<String> formats { 
        get { return _formats; }
        private set { _formats = value; }
    }

    public int format {get; set; } = 0;

    private delegate void WriteDate(StreamWriter log, DateTime date);
    private delegate void WriteUmatched(StreamWriter log, string name, DateTime time, bool warning);
    private delegate void WriteMatched(StreamWriter log, string name, DateTime start, DateTime end, bool warning);
    private delegate void WriteHours(StreamWriter log, string name, int minutes, float hourlyRate);

    private void export(List<Check> checks, string fileName, string extension, WriteDate? writeDate, WriteUmatched? writeUnmatched, WriteMatched? writeMatched,
     Action<StreamWriter>? writeLogStart, Action<StreamWriter>? writeHoursStart, WriteHours? writeHours) {
        if(!Directory.Exists("izvestaji")) Directory.CreateDirectory("izvestaji");
        StreamWriter log = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-log.{extension}");

        int tolerance = (int) Settings.getInt("tolerance")!;
        DateTime date = DateTime.MinValue;
        List<Check> unmatched = new List<Check>();
        Dictionary<Worker, int> minutes = new Dictionary<Worker, int>();
        Dictionary<Worker, TimeConfig?[]> timeConfigs = new Dictionary<Worker, TimeConfig?[]>();

        foreach (Worker worker in db.getWorkers()) {
            TimeConfig?[] timeConfig = new TimeConfig?[7];
            for(int i = 0; i < 7; i++) timeConfig[i] = db.GetTimeConfig(worker.timeConfig, i);
            timeConfigs.Add(worker, timeConfig);
            minutes.Add(worker, 0);
        }

        writeLogStart?.Invoke(log);
        foreach (Check check in checks) {
            if (check.time.Date != date.Date) {
                foreach (Check unmatchedCheck in unmatched) {
                    Worker worker = unmatchedCheck.worker;
                    int day = unmatchedCheck.time.DayOfWeek != DayOfWeek.Sunday ? (int) unmatchedCheck.time.DayOfWeek - 1 : 6;
                    bool warning = false;
                    if(timeConfigs[worker][day] != null) {
                        int difference = (unmatchedCheck.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                            + unmatchedCheck.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                        if(Math.Abs(difference) > tolerance) warning = true;
                    }

                    writeUnmatched?.Invoke(log, worker.firstName + " " + worker.lastName, unmatchedCheck.time, warning);
                }

                date = check.time;
                unmatched.Clear();
                writeDate?.Invoke(log, date);
            }
            
            Check? match = unmatched.Find(unmatchedCheck => unmatchedCheck.worker.id == check.worker.id);
            if(match == null) unmatched.Add(check);
            else {
                Worker worker = match.worker;
                int day = match.time.DayOfWeek != DayOfWeek.Sunday ? (int) match.time.DayOfWeek - 1 : 6;
                bool warning = false;
                DateTime startTime = match.time;
                DateTime endTime = check.time;

                if(timeConfigs[worker][day] != null) {
                    int difference = (match.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                        + match.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                    if(Math.Abs(difference) > tolerance) warning = true;
                    else startTime = DateTime.ParseExact($"{timeConfigs[worker][day]!.HourStart}:{timeConfigs[worker][day]!.MinuteStart}", "HH:mm", CultureInfo.InvariantCulture);
                    
                    difference = (check.time.Hour - int.Parse(timeConfigs[worker][day]!.HourEnd)) * 60
                        + check.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteEnd);
                    if(Math.Abs(difference) > tolerance) warning = true;
                    else endTime = DateTime.ParseExact($"{timeConfigs[worker][day]!.HourEnd}:{timeConfigs[worker][day]!.MinuteEnd}", "HH:mm", CultureInfo.InvariantCulture);
                }

                int time = (endTime.Hour - startTime.Hour) * 60 + endTime.Minute - startTime.Minute;
                minutes[match.worker] += time;

                writeMatched?.Invoke(log, match.worker.firstName + " " + match.worker.lastName, match.time, check.time, warning);
                unmatched.Remove(match);
            }
        }

        foreach (Check unmatchedCheck in unmatched){
            Worker worker = unmatchedCheck.worker;
            int day = unmatchedCheck.time.DayOfWeek != DayOfWeek.Sunday ? (int) unmatchedCheck.time.DayOfWeek - 1 : 6;
            bool warning = false;
            if(timeConfigs[worker][day] != null) {
                int difference = (unmatchedCheck.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                    + unmatchedCheck.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                if(Math.Abs(difference) > tolerance) warning = true;
            }

            writeUnmatched?.Invoke(log, worker.firstName + " " + worker.lastName, unmatchedCheck.time, warning);
        }

        log.Close();
        Logger.log($"Exported {extension} log");

        StreamWriter hours = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-sati.{extension}");
        writeHoursStart?.Invoke(hours);
        foreach(KeyValuePair<Worker, int> entry in minutes.OrderBy(entry => entry.Key.firstName + entry.Key.lastName)) 
            writeHours?.Invoke(hours, entry.Key.firstName + " " + entry.Key.lastName, entry.Value, entry.Key.hourlyRate);
        hours.Close();
        Logger.log($"Exported {extension} hour log");
    }

    public void exportClick() {
        if(format < 0 || format > _formats.Count) {
            Logger.log("Failed exporting: No format selected");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Niste izabrali korektan format.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        int _day = 1, _month = 1, _year = 2000;
        if((day.Length != 0 && (!int.TryParse(day, out _day) || _day < 1 || _day > 31)) || 
           (month.Length != 0 && (!int.TryParse(month, out _month) || _month < 1 || _month > 12)) || 
           !int.TryParse(year, out _year) || _year < 2000) {
            Logger.log($"Failed exporting: invalid date({year}.{month}.{day})");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Niste uneli pravilan datum!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }

        ExportPeriod period;
        if (month.Length == 0 && day.Length == 0) period = ExportPeriod.Year;
        else if (day.Length == 0) period = ExportPeriod.Month;
        else if (month.Length == 0) {
            Logger.log("Failed exporting: Month field empty, while Day field is not");
            MessageBoxManager.GetMessageBoxStandard("Greška", "Polje za mesec ne može da ostane prazno, a da je popunjeno polje za dan.",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            return;
        }
        else period = ExportPeriod.Day;

        string fileName = period switch {
            ExportPeriod.Year => $"{_year:0000}",
            ExportPeriod.Month => $"{_year:0000}-{_month:00}",
            _ => $"{_year:0000}-{_month:00}-{_day:00}",
        };

        List<Check> checks = db.getChecks(new DateTime(_year, _month, _day), period);
        switch(format) {
            case 0: export(checks, fileName, "txt",
                (log, date) => log.WriteLine($"\n{date:dd.MM.yyyy}\n"), 
                (log, name, time, warning) => log.WriteLine($"{(warning ? " !!! " : "")}{name} {time:HH:mm}-...\n"),
                (log, name, start, end, warning) => log.WriteLine($"{(warning ? " !!! " : "")}{name} {start:HH:mm}-{end:HH:mm}"), null, null,
                (log, name, minutes, hourlyRate) => log.WriteLine($"{name} {minutes / 60}h {minutes % 60}m - {minutes / 60f * hourlyRate}"));
                break;
            case 1: export(checks, fileName, "csv", 
                (log, date) => log.WriteLine(),
                (log, name, time, warning) => log.WriteLine($"{(warning ? "!!!" : "")},{name},{time:HH:mm},,{time:dd.MM.yyyy}"),
                (log, name, start, end, warning) => log.WriteLine($"{(warning ? "!!!" : "")},{name},{start:HH:mm},{end:HH:mm},{start:dd.MM.yyyy}"),
                (log) => log.WriteLine("Odstupanje,Ime i prezime,došao,otišao,datum"),
                (log) => log.WriteLine("Ime i prezime,sati,minuti,plata"),
                (log, name, minutes, hourlyRate) => log.WriteLine($"{name},{minutes / 60},{minutes % 60},{minutes / 60f * hourlyRate}"));
                break;
            default: throw new ArgumentException("Invalid format selected.");
        };

        MessageBoxManager.GetMessageBoxStandard("Izvezeno", "Uspešno je napravljen isveštaj.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
    }

    public ExportWindowViewModel(DatabaseInterface db) {
        this.db = db;
        _formats = new ObservableCollection<String>() {"txt", "csv"};
    }
}