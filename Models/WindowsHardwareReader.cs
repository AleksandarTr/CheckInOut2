using System;
using System.Runtime.InteropServices;

namespace CheckInOut2.Models;

public class WindowsHardwareReader : PlatformHardwareReader {
    private const int WM_INPUT = 0x00FF;
    private const int HID_USAGE_PAGE_GENERIC = 0x01;
    private const int HID_USAGE_GENERIC_KEYBOARD = 0x06;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int GWL_WNDPROC = -4;
    private const int RIM_TYPEKEYBOARD = 0x01;
    private const uint RID_INPUT = 0x10000003;
    private IntPtr _originalWndProc;
    private IntPtr _hwnd;

    public WindowsHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter, ulong hardwareID)
     : base(buffer, checkBufferWaiter, hardwareID) {
        _hwnd = FindWindow(null, "CheckInOut2");
        _originalWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, WndProc);
        RegisterRawInput();
    }

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

    private void RegisterRawInput()
    {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
        rid[0].UsagePage = HID_USAGE_PAGE_GENERIC; // Generic desktop controls
        rid[0].Usage = HID_USAGE_GENERIC_KEYBOARD; // Keyboard
        rid[0].Flags = RIDEV_INPUTSINK; // RIDEV_INPUTSINK, get input even when not focused
        rid[0].TargetWindow = _hwnd; // The handle of the target window

        if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
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
}