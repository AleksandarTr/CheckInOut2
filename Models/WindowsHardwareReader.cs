using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Management;

namespace CheckInOut2.Models;

public class WindowsHardwareReader : PlatformHardwareReader {
    private const int WM_INPUT = 0x00FF;
    private const int HID_USAGE_PAGE_GENERIC = 0x01;
    private const int HID_USAGE_GENERIC_KEYBOARD = 0x06;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int GWL_WNDPROC = -4;
    private const int RIM_TYPEKEYBOARD = 0x01;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIDI_DEVICENAME = 0x20000007;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
    private IntPtr _originalWndProc;
    private IntPtr _hwnd;
    private WndProcDelegate wndProcMethod;
    private bool active = false;

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string windowClass, string windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Delegate declaration for the window procedure
    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr TargetWindow;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct RAWINPUT
    {
        [FieldOffset(0)]
        public RAWINPUTHEADER header;
        [FieldOffset(16)]
        public RAWKEYBOARD keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [StructLayout(LayoutKind.Sequential)]
    struct RAWINPUTDEVICELIST {
        public IntPtr hDevice;
        public int dwType;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateFile(string fileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetProductString(IntPtr HidDeviceObject, byte[] buffer, ulong bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("hid.dll", SetLastError = true)]
    static extern bool HidD_GetAttributes(IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

    [StructLayout(LayoutKind.Sequential)]
    struct HIDD_ATTRIBUTES
    {
        public int Size;
        public ushort VendorID;
        public ushort ProductID;
        public ushort VersionNumber;
    }

    public WindowsHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter)
     : base(buffer, checkBufferWaiter) {
        _hwnd = FindWindow(null, "CheckInOut2");
        wndProcMethod = WndProc;
        _originalWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, wndProcMethod);
        RegisterRawInput();
        List<Device> devices = getDeviceList();

        Device reader = devices.Find(device => device.serialNumber == ulong.Parse(Settings.get("readerID")!));
        if(reader.name == null) raiseError("Izabrani uređaj nije povezan na računar.");
        else {
            hardwareID = reader.hardwareID;
            active = true;
        }

        ManagementEventWatcher watcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%keyboard%'"));

        watcher.EventArrived += new EventArrivedEventHandler(DeviceInserted);
        watcher.Start();

        // Query for device removal events
        ManagementEventWatcher removalWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%keyboard%'"));

        removalWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemoved);
        removalWatcher.Start();
    }

    private void RegisterRawInput()
    {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
        rid[0].UsagePage = HID_USAGE_PAGE_GENERIC; // Generic desktop controls
        rid[0].Usage = HID_USAGE_GENERIC_KEYBOARD; // Keyboard
        rid[0].Flags = RIDEV_INPUTSINK; // RIDEV_INPUTSINK, get input even when not focused
        rid[0].TargetWindow = _hwnd; // The handle of the target window

        if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            raiseError(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message);
    }

    private IntPtr WndProc (IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if(msg == WM_INPUT && lParam != IntPtr.Zero || _bufferPtr == 100) {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            
            if(dwSize == 0) return CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
            IntPtr rawBuffer = Marshal.AllocHGlobal((int) dwSize);

            if(GetRawInputData(lParam, RID_INPUT, rawBuffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
                return CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
            RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(rawBuffer);

            if(raw.header.dwType != RIM_TYPEKEYBOARD || (ulong) raw.header.hDevice != hardwareID || raw.keyboard.Message < 2 || raw.keyboard.Message > 11) 
                return CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);

            lock(buffer) {
                if(raw.keyboard.Message == 11) buffer[_bufferPtr++] = 0;
                else buffer[_bufferPtr++] = (byte)(raw.keyboard.Message - 1);
                checkBufferWaiter();
            }
        }

        return CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
    }

    private static string getHumanReadableName(string path) {
        IntPtr HIDHandle = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if(HIDHandle != IntPtr.Zero)
        {
            byte[] buffer = new byte[126];
            bool result = HidD_GetProductString(HIDHandle, buffer, 126);
            if(!result) return "Nepoznatno ime";
            CloseHandle(HIDHandle);
            string name = Encoding.Unicode.GetString(buffer).TrimEnd('\0');
            if(name.Length > 0) return name;
        }
        return "Nepoznatno ime";
    }

    private static string getSerialNumber(string path) {
        IntPtr HIDHandle = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        
        HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
        attributes.Size = Marshal.SizeOf(attributes);

        if (HidD_GetAttributes(HIDHandle, ref attributes))
            return attributes.VendorID.ToString() + attributes.VersionNumber.ToString();

        return "";
    }

    public override List<Device> getDeviceList()
    {
        List<Device> devices = new List<Device>();

        uint deviceCount = 0;
        uint dwSize = (uint) Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
        GetRawInputDeviceList(null, ref deviceCount, dwSize);

        RAWINPUTDEVICELIST[] rawDevices = new RAWINPUTDEVICELIST[deviceCount];
        GetRawInputDeviceList(rawDevices, ref deviceCount, dwSize);

        foreach(RAWINPUTDEVICELIST rawDevice in rawDevices) {
            if(rawDevice.dwType != RIM_TYPEKEYBOARD) continue;
            uint nameSize = 0;
            GetRawInputDeviceInfo(rawDevice.hDevice, RIDI_DEVICENAME, null, ref nameSize);

            if(nameSize > 0) {
                StringBuilder deviceName = new StringBuilder((int) nameSize);
                GetRawInputDeviceInfo(rawDevice.hDevice, RIDI_DEVICENAME, deviceName, ref nameSize);

                string serialNumber = getSerialNumber(deviceName.ToString());
                devices.Add(new Device() {
                    name = getHumanReadableName(deviceName.ToString()),
                    hardwareID = (ulong) rawDevice.hDevice,
                    serialNumber = serialNumber != "" ? ulong.Parse(serialNumber) : (ulong) rawDevice.hDevice
                });
            } 
        }

        return devices;
    }

    private void DeviceInserted(object sender, EventArrivedEventArgs e)
    {
        Device reader = getDeviceList().Find(device => device.serialNumber == ulong.Parse(Settings.get("readerID")!));
        if(reader.name != null && !active) {
            addMessage("Čitač je ponovo povezan.");
            hardwareID = reader.hardwareID;
            active = true;
        }
    }

    private void DeviceRemoved(object sender, EventArrivedEventArgs e)
    {
        Device reader = getDeviceList().Find(device => device.serialNumber == ulong.Parse(Settings.get("readerID")!));
        if(reader.name == null && active) {
            active = false;
            raiseError("Čitač više nije povezan na računar.");
        }
    }

    override public void updateHardwareId() {
        Device reader = getDeviceList().Find(device => device.serialNumber == ulong.Parse(Settings.get("readerID")!));
        if(reader.name != null) {
            if(!active) addMessage("Čitač je povezan");
            hardwareID = reader.hardwareID;
            active = true;
        }
        else if(!active) raiseError("Nije bilo moguće povezati čitač");
    }
}