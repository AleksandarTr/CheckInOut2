using Avalonia.Controls;

namespace CheckInOut2.Models;

public delegate void ChipReaderEventHandler(string chip);

public static class ChipReader {
    private static event ChipReaderEventHandler chipReaderEvent;
    private static Window? activeWindow = null;

    public static void addChipReaderEventHandler(ChipReaderEventHandler chipReaderEventHandler) {
        chipReaderEvent += chipReaderEventHandler;
    }

    public static void removeChipReaderEventHandler(ChipReaderEventHandler chipReaderEventHandler) {
        chipReaderEvent -= chipReaderEventHandler;
    }

    public static void focusWindow(Window window) {
        activeWindow = window;
    }

    public static bool isFocused(Window window) {
        return activeWindow == window;
    }

    public static void readChip(string chip) {
        chipReaderEvent.Invoke(chip);
    }
}