using System;
using System.Collections.Generic;
using Avalonia.Threading;
using CheckInOut2.Views;

namespace CheckInOut2.Models;

public delegate void checkBufferWaiterDelegate();

public abstract class PlatformHardwareReader {
    protected byte[] buffer;
    protected byte _bufferPtr = 0;
    protected checkBufferWaiterDelegate checkBufferWaiter;
    protected ulong hardwareID;
    public static PlatformHardwareReader instance {
        get; private set;
    }
    public byte bufferPtr {
        get { 
            byte temp = _bufferPtr;
            _bufferPtr = 0;
            return temp;
        }
    }

    public struct Device {
        public string name;
        public ulong hardwareID;
        public ulong serialNumber;
    }

    public abstract List<Device> getDeviceList();

    protected void raiseError(string error) {
        Dispatcher.UIThread.InvokeAsync(new Action(() =>
            MainWindow.addMessage($"Greška kod čitača:{error}", "#f73434"
        )));
        Logger.log("Chip reader error:" + error);
    }

    protected void addMessage(string message) {
        Dispatcher.UIThread.InvokeAsync(new Action(() =>
            MainWindow.addMessage($"{message}", "#42d685"
        )));
        Logger.log("Chip reader message:" + message);
    }

    public PlatformHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter) {
        this.buffer = buffer;
        this.checkBufferWaiter = checkBufferWaiter;
        instance = this;
    }

    public abstract void updateHardwareId();
}