using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace CheckInOut2.Models;

public class LinuxHardwareReader : PlatformHardwareReader {
    private const int EV_KEY = 0x01;
    private const int O_RDONLY = 0x0000;
    private const int O_NONBLOCK = 0x800;
    private const short POLLIN = 0x0001;
    private const uint EVIOCGRAB = 0x40044590;

    [StructLayout(LayoutKind.Sequential)]
    struct PollFd
    {
        public int fd;
        public short events;
        public short revents;
    }

    public LinuxHardwareReader(byte[] buffer, checkBufferWaiterDelegate checkBufferWaiter)
     : base(buffer, checkBufferWaiter) {
        hardwareID = ulong.Parse(Settings.get("readerID")!);
        new Thread(new ThreadStart(ChipReaderHandler)).Start();
     }

    [DllImport("libc", SetLastError = true)]
    private static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, uint request, IntPtr argp);

    [DllImport("libc", SetLastError = true)]
    private static extern int read(int fd, byte[] buffer, int count);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int poll(ref PollFd fds, int nfds, int timeout);

    private void ChipReaderHandler()
    {
        string devicePath = "/dev/input/event" + hardwareID.ToString();  // Replace with your actual device path
        int fd = open(devicePath, O_RDONLY | O_NONBLOCK);

        if (fd < 0)
        {
            raiseError($"Nije moglo da se pristupi uređaju({Marshal.GetLastWin32Error()}).");
            return;
        }

        // Grab the device
        if (ioctl(fd, EVIOCGRAB, 1) < 0)
            raiseError($"Nije moglo da se preuzme vlasništvo nad uređajem({Marshal.GetLastWin32Error()}).");
    
        PollFd pollfd = new PollFd
        {
            fd = fd,
            events = POLLIN
        };

        byte[] buffer = new byte[24];  // evdev event structure size

        while (true)
        {
            int pollResult = poll(ref pollfd, 1, -1);  // Wait indefinitely (-1) for an event
            if (pollResult > 0 && (pollfd.revents & POLLIN) != 0)
            {
                int bytesRead = read(fd, buffer, buffer.Length);
                if (bytesRead > 0)
                {
                    // Parse the event structure
                    int type = BitConverter.ToInt16(buffer, 16);
                    int code = BitConverter.ToInt16(buffer, 18);
                    int value = BitConverter.ToInt32(buffer, 20);

                    if (type == EV_KEY && value == 1 && code >= 2 && code <= 11) // Key press event
                    {
                        if(code == 11) this.buffer[_bufferPtr++] = 0;
                        else this.buffer[_bufferPtr++] = (byte) (code - 1);
                        checkBufferWaiter();
                    }
                }
            }
        }
    }

    [DllImport("libudev.so.1", EntryPoint = "udev_new")]
    private static extern IntPtr udev_new();

    [DllImport("libudev.so.1", EntryPoint = "udev_unref")]
    private static extern IntPtr udev_unref(IntPtr udev);

    [DllImport("libudev.so.1", EntryPoint = "udev_enumerate_new")]
    private static extern IntPtr udev_enumerate_new(IntPtr udev);

    [DllImport("libudev.so.1", EntryPoint = "udev_enumerate_unref")]
    private static extern IntPtr udev_enumerate_unref(IntPtr enumerate);

    [DllImport("libudev.so.1", EntryPoint = "udev_enumerate_scan_devices")]
    private static extern int udev_enumerate_scan_devices(IntPtr enumerate);

    [DllImport("libudev.so.1", EntryPoint = "udev_enumerate_get_list_entry")]
    private static extern IntPtr udev_enumerate_get_list_entry(IntPtr enumerate);

    [DllImport("libudev.so.1", EntryPoint = "udev_list_entry_get_next")]
    private static extern IntPtr udev_list_entry_get_next(IntPtr list_entry);

    [DllImport("libudev.so.1", EntryPoint = "udev_list_entry_get_name")]
    private static extern IntPtr udev_list_entry_get_name(IntPtr list_entry);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_new_from_syspath")]
    private static extern IntPtr udev_device_new_from_syspath(IntPtr udev, string syspath);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_get_devnode")]
    private static extern IntPtr udev_device_get_devnode(IntPtr device);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_get_subsystem")]
    private static extern IntPtr udev_device_get_subsystem(IntPtr device);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_get_property_value")]
    private static extern IntPtr udev_device_get_property_value(IntPtr device, string key);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_get_sysattr_value")]
    private static extern IntPtr udev_device_get_sysattr_value(IntPtr device, string attr);

    [DllImport("libudev.so.1", EntryPoint = "udev_device_unref")]
    private static extern IntPtr udev_device_unref(IntPtr device);

    public override List<Device> getDeviceList()
    {
        List<Device> result = new List<Device>();
        IntPtr udev = udev_new();
        if (udev == IntPtr.Zero)
        {
            raiseError($"Nije moglo da se pristupi listi uređaja({Marshal.GetLastWin32Error()})");
            return new List<Device>();
        }

        IntPtr enumerate = udev_enumerate_new(udev);
        udev_enumerate_scan_devices(enumerate);
        IntPtr devices = udev_enumerate_get_list_entry(enumerate);

        IntPtr entry = devices;
        while (entry != IntPtr.Zero)
        {
            string syspath = Marshal.PtrToStringAnsi(udev_list_entry_get_name(entry))!;
            IntPtr device = udev_device_new_from_syspath(udev, syspath);

            string subsystem = Marshal.PtrToStringAnsi(udev_device_get_subsystem(device))!;
            string? devnode = Marshal.PtrToStringAnsi(udev_device_get_devnode(device));
            string? devtype = Marshal.PtrToStringAnsi(udev_device_get_property_value(device, "ID_INPUT_KEYBOARD"));

            if (subsystem == "input" && devtype != null && devnode != null && devnode.StartsWith("/dev/input/event"))
            {
                string? name = Marshal.PtrToStringAnsi(udev_device_get_sysattr_value(device, "name"));

                if (name == null)
                {
                    string vendor = Marshal.PtrToStringAnsi(udev_device_get_property_value(device, "ID_VENDOR"))!;
                    string model = Marshal.PtrToStringAnsi(udev_device_get_property_value(device, "ID_MODEL"))!;

                    name = $"{vendor} {model}".Trim();
                }

                result.Add(new Device() {
                    name = name ?? "Nepoznato ime",
                    hardwareID = ulong.Parse(devnode.Replace("/dev/input/event", "")),
                    serialNumber = ulong.Parse(devnode.Replace("/dev/input/event", ""))
                });
            }

            udev_device_unref(device);
            entry = udev_list_entry_get_next(entry);
        }

        udev_enumerate_unref(enumerate);
        udev_unref(udev);
        return result;
    }
}