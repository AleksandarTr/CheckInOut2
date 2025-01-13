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
            MainWindow.addMessage($"Greška kod čitača:{error}"
        )));
        Logger.log("Chip reader error:" + error);
    }

    public PlatformHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter) {
        this.buffer = buffer;
        this.checkBufferWaiter = checkBufferWaiter;
        instance = this;
    }
}