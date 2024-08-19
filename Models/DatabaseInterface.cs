using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace CheckInOut2.Models;

public class DatabaseInterface {
    private SqliteConnection connection;

    public DatabaseInterface(String location) {
        connection = new SqliteConnection("Data Source=" + location);
        connection.Open();
    }

    ~DatabaseInterface() {
        connection.Close();
    }
}