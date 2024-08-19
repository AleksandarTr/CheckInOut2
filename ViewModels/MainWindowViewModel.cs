using System;
using System.Collections.Generic;
using CheckInOut2.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CheckInOut2.ViewModels;

public delegate bool ReaderEventHandler(String message);

public partial class MainWindowViewModel : ObservableObject
{
    private event ReaderEventHandler? readerInput;
    private DatabaseInterface db;

    public void addReaderEventHandler(ReaderEventHandler handler) {
        readerInput += handler;
    }

    public MainWindowViewModel() {
        db = new DatabaseInterface("checkIO.db");
    }
}
