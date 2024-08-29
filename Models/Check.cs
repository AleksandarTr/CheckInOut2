using System;

namespace CheckInOut2.Models;

public class Check {
    public int id;
    public DateTime time;
    public Worker worker = new Worker();
}