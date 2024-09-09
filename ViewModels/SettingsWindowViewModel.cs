using System.Collections.Generic;
using System.Collections.ObjectModel;
using CheckInOut2.Models;
using MsBox.Avalonia;

namespace CheckInOut2.ViewModels;

class SettingsWindowViewModel {
    private List<PlatformHardwareReader.Device> devices;
    private ObservableCollection<string> _readers = new ObservableCollection<string>();
    public ObservableCollection<string> readers {
        get { return _readers; }
        private set { _readers = value; }
    }
    public int reader {get; set;} = -1;
    public string fontSize {get; set; } = Settings.get("fontSize")!;
    public string tolerance {get; set; } = Settings.get("tolerance")!;

    public void saveSettings() {
        if(!int.TryParse(fontSize, out _)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nepravilna vrednost za veličinu fonta.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            return;
        }

        if(!int.TryParse(tolerance, out _)) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nepravilna vrednost za toleranciju kašnjenja.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            return;
        }

        if(reader < 0 || reader >= readers.Count) {
            MessageBoxManager.GetMessageBoxStandard("Greška", "Nepravilna vrednost za čitač.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
            return;
        }

        if(Settings.get("fontSize") != fontSize) Logger.log($"fontSize changed from {Settings.get("fontSize")} to {fontSize}");
        Settings.set("fontSize", fontSize);

        if(Settings.get("tolerance") != tolerance) Logger.log($"tolerance changed from {Settings.get("tolerance")} to {tolerance}");
        Settings.set("tolerance", tolerance);

        string newReaderID = devices[reader].hardwareID.ToString();
        if(Settings.get("readerID") != newReaderID) Logger.log($"readerID changed from {Settings.get("readerID")} to {newReaderID}");
        Settings.set("readerID", devices[reader].hardwareID.ToString());
        
        Settings.save();
        Logger.log("Settings saved");
        MessageBoxManager.GetMessageBoxStandard("Podešavanja", "Uspešno su sačuvana podešavanja. Moraćete da restartujete aplikaciju da bi sva bila primenjena.",
            MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info).ShowAsync();
    }

    public SettingsWindowViewModel() {
        devices = PlatformHardwareReader.instance.getDeviceList();
        devices.Sort((device1, device2) => device1.hardwareID > device2.hardwareID ? 1 : -1);
        devices.ForEach(device => readers.Add(device.name));
        string? readerID = Settings.get("readerID");
        if(readerID != null) {
            ulong hDevice = ulong.Parse(readerID);
            reader = devices.FindIndex(device => device.hardwareID == hDevice);
        }
    }
}