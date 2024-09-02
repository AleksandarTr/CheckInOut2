namespace CheckInOut2.Models;

public delegate void checkBufferWaiterDelegate();

public abstract class PlatformHardwareReader {
    protected byte[] buffer;
    protected byte _bufferPtr = 0;
    protected checkBufferWaiterDelegate checkBufferWaiter;
    protected readonly ulong hardwareID;
    public byte bufferPtr {
        get { 
            byte temp = _bufferPtr;
            _bufferPtr = 0;
            return temp;
        }
    }

    public PlatformHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter, ulong hardwareID) {
        this.buffer = buffer;
        this.checkBufferWaiter = checkBufferWaiter;
        this.hardwareID = hardwareID;
    }
}