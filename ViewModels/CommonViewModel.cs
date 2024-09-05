using CheckInOut2.Models;

namespace CheckInOut2.ViewModels;

class CommonViewModel {
    public int fontSize {get; set;} = int.Parse(Settings.get("fontSize")!);
}