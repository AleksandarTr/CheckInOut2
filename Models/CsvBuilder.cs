using System;
using System.Collections.Generic;
using System.IO;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Models;

class CsvBuilder : LogBuilder {
    private StreamWriter log;
    private StreamWriter hours;

    public CsvBuilder(DateTime exportDate, ExportPeriod period) : base(exportDate, period) {
        if(!Directory.Exists("izvestaji")) Directory.CreateDirectory("izvestaji");
        log = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-dnevnik.csv");
        hours = File.CreateText($"izvestaji{Path.DirectorySeparatorChar}{fileName}-sati.csv");
    }

    public override void writeDate(DateTime date)
    {
        log.WriteLine();
    }

    public override void writeUnmatched(string name, DateTime time, bool warning)
    {
        log.WriteLine($"{(warning ? "!!!" : "")},{name},{time:HH:mm},,{time:dd.MM.yyyy}");
    }

    public override void writeMatched(string name, DateTime start, DateTime end, bool warning)
    {
        log.WriteLine($"{(warning ? "!!!" : "")},{name},{start:HH:mm},{end:HH:mm},{start:dd.MM.yyyy}");
    }

    public override void writeLogStart()
    {
        log.WriteLine("Odstupanje,Ime i prezime,došao,otišao,datum");
    }

    public override void writeHoursStart()
    {
        hours.WriteLine("Ime i prezime,sati,minuti,plata");
    }

    public override void writeHours(string name, int minutes, int expectedMinutes, float hourlyRate, float salary)
    {
        hours.WriteLine($"{name},{minutes / 60},{minutes % 60},{salary + (minutes - expectedMinutes) / 60f * hourlyRate}");
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