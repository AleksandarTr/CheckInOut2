using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.Models;

public partial class TimeConfig : ObservableObject {
    public int id = -1;
    public int day = -1;
    [ObservableProperty]
    private string _hourStart = "";
    [ObservableProperty]
    private string _minuteStart = "";
    [ObservableProperty]
    private string _hourEnd = "";
    [ObservableProperty]
    private string _minuteEnd = "";

    public static List<string> ToStrings(List<TimeConfig> dayConfigs) {
        List<string> result = new List<string>();

        if(dayConfigs.Count == 0) return result;
        int currentId = dayConfigs[0].id;
        string timeConfigString = "";
        int day = 0;

        foreach(TimeConfig dayConfig in dayConfigs) {
            if(currentId != dayConfig.id) {
                while(day < 7) {
                    timeConfigString += "---/";
                    day++;
                }
                result.Add(timeConfigString);
                timeConfigString = "";
                day = 0;
            }

            while(day != dayConfig.day) {
                timeConfigString += "---/";
                day++;
            }
            
            timeConfigString += $"{dayConfig.HourStart:00}:{dayConfig.MinuteStart:00}-{dayConfig.HourEnd:00}:{dayConfig.MinuteEnd:00}/";
            day++;
        }

        while(day < 7) {
            timeConfigString += "---/";
            day++;
        }
        result.Add(timeConfigString);
        return result;
    }
}