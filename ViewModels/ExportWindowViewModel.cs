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

    private void export(List<Check> checks, DateTime exportDate, ExportPeriod period, LogBuilder logBuilder) {
        int toleranceEarly = (int) Settings.getInt("toleranceEarly")!;
        int toleranceLate = (int) Settings.getInt("toleranceLate")!;
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

        logBuilder.writeLogStart();
        foreach (Check check in checks) {
            if (check.time.Date != date.Date) {
                foreach (Check unmatchedCheck in unmatched) {
                    Worker worker = unmatchedCheck.worker;
                    int day = getDay(unmatchedCheck.time);
                    bool warning = false;
                    if(timeConfigs[worker][day] != null) {
                        int difference = (unmatchedCheck.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                            + unmatchedCheck.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                        if(-difference > toleranceEarly || difference > toleranceLate) warning = true;
                    }

                    logBuilder.writeUnmatched(worker.firstName + " " + worker.lastName, unmatchedCheck.time, warning);
                }

                date = check.time;
                unmatched.Clear();
                logBuilder.writeDate(date);
            }
            
            Check? match = unmatched.Find(unmatchedCheck => unmatchedCheck.worker.id == check.worker.id);
            if(match == null) unmatched.Add(check);
            else {
                Worker worker = match.worker;
                int day = getDay(match.time);
                bool warning = false;
                DateTime startTime = match.time;
                DateTime endTime = check.time;

                if(timeConfigs[worker][day] != null) {
                    int difference = (match.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                        + match.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                    if(-difference > toleranceEarly || difference > toleranceLate) warning = true;
                    else startTime = DateTime.ParseExact($"{timeConfigs[worker][day]!.HourStart}:{timeConfigs[worker][day]!.MinuteStart}", "HH:mm", CultureInfo.InvariantCulture);
                    
                    difference = (check.time.Hour - int.Parse(timeConfigs[worker][day]!.HourEnd)) * 60
                        + check.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteEnd);
                    if(difference > toleranceEarly || -difference > toleranceLate) warning = true;
                    else endTime = DateTime.ParseExact($"{timeConfigs[worker][day]!.HourEnd}:{timeConfigs[worker][day]!.MinuteEnd}", "HH:mm", CultureInfo.InvariantCulture);
                }

                int time = (endTime.Hour - startTime.Hour) * 60 + endTime.Minute - startTime.Minute;
                minutes[match.worker] += time;

                logBuilder.writeMatched(match.worker.firstName + " " + match.worker.lastName, match.time, check.time, warning);
                unmatched.Remove(match);
            }
        }

        foreach (Check unmatchedCheck in unmatched){
            Worker worker = unmatchedCheck.worker;
            int day = getDay(unmatchedCheck.time);
            bool warning = false;
            if(timeConfigs[worker][day] != null) {
                int difference = (unmatchedCheck.time.Hour - int.Parse(timeConfigs[worker][day]!.HourStart)) * 60
                    + unmatchedCheck.time.Minute - int.Parse(timeConfigs[worker][day]!.MinuteStart);
                if(-difference > toleranceEarly || difference > toleranceLate) warning = true;
            }

            logBuilder.writeUnmatched(worker.firstName + " " + worker.lastName, unmatchedCheck.time, warning);
        }

        logBuilder.saveLogFile();
        Logger.log($"Exported log");

        logBuilder.writeHoursStart();
        foreach(KeyValuePair<Worker, int> entry in minutes.OrderBy(entry => entry.Key.firstName + entry.Key.lastName)) 
            logBuilder.writeHours(entry.Key.firstName + " " + entry.Key.lastName, entry.Value, getExpectedWorkTime(entry.Key, period, exportDate, out float expectedSalary), entry.Key.hourlyRate, expectedSalary);
        logBuilder.saveHoursFile();
        Logger.log($"Exported hour log");
    }

    private int getDay(DateTime date) {
        return date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int) date.DayOfWeek - 1;
    }

    private int getExpectedWorkTimeInMonth(TimeConfig?[] timeConfigs, int year, int month) {
        int expectedMinutes = 0;
        int days = DateTime.DaysInMonth(year, month);
        for(int i = 1; i <= days; i++) {
            int dayOfMonth = getDay(new DateTime(year, month, i));
            if(timeConfigs[dayOfMonth] == null) continue;
            TimeConfig config = timeConfigs[dayOfMonth]!;
            expectedMinutes += (int.Parse(config.HourEnd) - int.Parse(config.HourStart)) * 60 + int.Parse(config.MinuteEnd) - int.Parse(config.MinuteStart);
        }

        return expectedMinutes;
    }

    private int getExpectedWorkTime(Worker worker, ExportPeriod period, DateTime date, out float expectedSalary) {
        TimeConfig?[] timeConfigs = new TimeConfig?[7];
        for(int i = 0; i < 7; i++) timeConfigs[i] = db.GetTimeConfig(worker.timeConfig, i);

        if(period == ExportPeriod.Year) {
            int result = 0;
            for(int i = 1; i <= 12; i++) result += getExpectedWorkTimeInMonth(timeConfigs, date.Year, i);
            expectedSalary = worker.salary * 12;
            return result;
        }

        int expectedMinutes = getExpectedWorkTimeInMonth(timeConfigs, date.Year, date.Month);
        if(period == ExportPeriod.Month) {
            expectedSalary = worker.salary;
            return expectedMinutes;
        }

        int day = getDay(date);
        if(timeConfigs[day] == null) {
            expectedSalary = 0;
            return 0;
        }

        TimeConfig config = timeConfigs[day]!;
        int expectedTime = (int.Parse(config.HourEnd) - int.Parse(config.HourStart)) * 60 + int.Parse(config.MinuteEnd) - int.Parse(config.MinuteStart);
        expectedSalary = 1f * expectedTime / expectedMinutes * worker.salary;
        return expectedTime;
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

        DateTime date = new DateTime(_year, _month, _day);
        List<Check> checks = db.getChecks(new DateTime(_year, _month, _day), period);

        switch(format) {
            case 0: export(checks, date, period, new TxtBuilder(date, period)); break;
            case 1: export(checks, date, period, new CsvBuilder(date, period)); break;
            default: throw new ArgumentException("Invalid format selected.");
        };

        MessageBoxManager.GetMessageBoxStandard("Izvezeno", "Uspešno je napravljen isveštaj.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
    }

    public ExportWindowViewModel(DatabaseInterface db) {
        this.db = db;
        _formats = new ObservableCollection<String>() {"txt", "csv"};
    }
}