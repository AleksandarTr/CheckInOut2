using System.Collections.Generic;

namespace CheckInOut2.Models;

public delegate void checkBufferWaiterDelegate();

public abstract class PlatformHardwareReader {
    protected byte[] buffer;
    protected byte _bufferPtr = 0;
    protected checkBufferWaiterDelegate checkBufferWaiter;
    protected readonly ulong hardwareID;
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
    }

    public abstract List<Device> getDeviceList();

    public PlatformHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter, ulong hardwareID) {
        this.buffer = buffer;
        this.checkBufferWaiter = checkBufferWaiter;
        this.hardwareID = hardwareID;
        instance = this;
    }
}