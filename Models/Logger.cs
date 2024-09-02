using System;
using System.IO;

namespace CheckInOut2.Models;

public static class Logger { 
    private static StreamWriter logger = new StreamWriter("log.txt", true);

    private static void loggerTerminateHandler(object sender, UnhandledExceptionEventArgs args) {
        log($"{sender}:{args.ExceptionObject}");
    }

    static Logger() {
        logger.AutoFlush = true;
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(loggerTerminateHandler);
        log("");
        log("Application started at " + DateTime.Now.ToString("dd.MM.yyyy-HH:mm"));
    }

    public static void log(string message) {
        logger.WriteLine(message);
    }
}