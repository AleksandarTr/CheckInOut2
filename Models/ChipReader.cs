using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace CheckInOut2.Models;

public delegate void ChipReaderEventHandler(string chip);

public static class ChipReader {
    private static event ChipReaderEventHandler? chipReaderEvent;
    private static Window? activeWindow = null;
    private static List<Window> previouslyFocused = new List<Window>();

    public static void addChipReaderEventHandler(ChipReaderEventHandler chipReaderEventHandler) {
        chipReaderEvent += chipReaderEventHandler;
    }

    public static void removeChipReaderEventHandler(ChipReaderEventHandler chipReaderEventHandler) {
        chipReaderEvent -= chipReaderEventHandler;
    }

    public static void focusWindow(Window window) {
        if(activeWindow != null) previouslyFocused.Add(activeWindow);
        activeWindow = window;
    }

    public static bool isFocused(Window window) {
        return activeWindow == window;
    }

    public static void unfocus() {
        if(previouslyFocused.Count > 0) {
            activeWindow = previouslyFocused.Last();
            previouslyFocused.RemoveAt(previouslyFocused.Count - 1);
        }
        else activeWindow = null;
    }

    public static void readChip(string chip) {
        chipReaderEvent?.Invoke(chip);
    }

    static ChipReader() {
        #if WINDOWS
        new WindowsHardwareReader(buffer, checkBufferWaiter, ulong.Parse(Settings.get("readerID")!));
        #elif LINUX
        new LinuxHardwareReader(buffer, checkBufferWaiter, ulong.Parse(Settings.get("readerID")!));
        #endif
    }

    private static byte[] buffer = new byte[100];
    private static Thread? bufferWaiter = null;
    private const int bufferWaiterTimeout = 1000;

    private static void bufferWaiterBody() {
        Thread.Sleep(bufferWaiterTimeout);
        StringBuilder chip = new StringBuilder();
        lock (buffer) {
            byte charCount = PlatformHardwareReader.instance.bufferPtr;
            for(int i = 0; i < charCount; i++) chip.Append(buffer[i]);
            bufferWaiter = null;
        }
        Dispatcher.UIThread.InvokeAsync(new Action(() => {
            Logger.log("Read:" + chip.ToString());
            chipReaderEvent?.Invoke(chip.ToString());
        }));
    }

    private static void checkBufferWaiter() {
        if(bufferWaiter == null) {
            bufferWaiter = new Thread(new ThreadStart(bufferWaiterBody));
            bufferWaiter.Start();
        }
    }
}