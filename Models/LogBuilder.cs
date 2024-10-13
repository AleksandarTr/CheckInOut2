using System;
using System.Collections.Generic;
using CheckInOut2.ViewModels;

namespace CheckInOut2.Models;

abstract class LogBuilder {
    protected string fileName;

    public LogBuilder(DateTime exportDate, ExportPeriod period) {
        fileName = period switch {
            ExportPeriod.Year => exportDate.ToString("yyyy"),
            ExportPeriod.Month => exportDate.ToString("yyyy-MM"),
            _ => exportDate.ToString("yyyy-MM-dd"),
        };
    }

    virtual public void writeLogStart() {}

    virtual public void writeDate(DateTime date) {}

    abstract public void writeUnmatched(string name, DateTime time, bool warning);

    abstract public void writeMatched(string name, DateTime start, DateTime end, bool warning);

    abstract public void saveLogFile();

    virtual public void writeHoursStart() {}

    abstract public void writeHours(string name, int minutes, int expectedMinutes, float hourlyRate, float salary);

    abstract public void saveHoursFile();
}