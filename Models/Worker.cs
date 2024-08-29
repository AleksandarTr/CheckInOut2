namespace CheckInOut2.Models;

public class Worker {
    public int id = -1;
    public string firstName = "";
    public string lastName = "";
    public string chip = "";

    public override bool Equals(object? obj)
    {
        if(obj is not Worker || obj is null) return false;
        Worker worker = (obj as Worker)!;
        return id == worker.id && firstName == worker.firstName && lastName == worker.lastName && chip == worker.chip;
    }

    public override int GetHashCode() {
        return id;
    }
}