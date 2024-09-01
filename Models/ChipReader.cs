using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

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
}