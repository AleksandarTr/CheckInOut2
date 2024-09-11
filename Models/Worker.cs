namespace CheckInOut2.Models;

public class Worker {
    public int id = -1;
    public string firstName = "";
    public string lastName = "";
    public string chip = "";
    public float hourlyRate = 0;
    public int timeConfig = 0;
    public float salary = 0;

    public override bool Equals(object? obj)
    {
        if(obj is not Worker || obj is null) return false;
        Worker worker = (obj as Worker)!;
        return id == worker.id;
    }

    public override int GetHashCode() {
        return id;
    }
}