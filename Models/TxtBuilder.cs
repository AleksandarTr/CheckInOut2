namespace CheckInOut2.Models;

using System;
using System.Collections.Generic;
using System.IO;
using CheckInOut2.ViewModels;

class TxtBuilder : LogBuilder {
    private StreamWriter log;
    private StreamWriter hours;

    public TxtBuilder(DateTime exportDate, ExportPeriod period) : base(exportDate, period) {
        if(!Directory.Exists("izvestaji")) Directory.CreateDirectory("izvestaji");
        log = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-dnevnik.txt");
        hours = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-sati.txt");
    }

    public override void writeDate(DateTime date) {
        log.WriteLine($"\n{date:dd.MM.yyyy}\n");
    }

    public override void writeUnmatched(string name, DateTime time, bool warning)
    {
        log.WriteLine($"{(warning ? " !!! " : "")}{name} {time:HH:mm}-...\n");
    }

    public override void writeMatched(string name, DateTime start, DateTime end, bool warning)
    {
        log.WriteLine($"{(warning ? " !!! " : "")}{name} {start:HH:mm}-{end:HH:mm}");
    }

    public override void writeHours(string name, int minutes, int expectedMinutes, float hourlyRate, float salary)
    {
        hours.WriteLine($"{name} {minutes / 60}h {minutes % 60}m - {salary + (minutes - expectedMinutes) / 60f * hourlyRate}");
    }

    public override void saveLogFile()
    {
        log.Close();
    }

    public override void saveHoursFile()
    {
        hours.Close();
    }
}