using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CheckInOut2.Models;

public class DatabaseInterface {
    private SqliteConnection connection;

    private void createDatabase() {
        String createConnectionString = new SqliteConnectionStringBuilder(connection.ConnectionString){
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
        
        connection = new SqliteConnection(createConnectionString);
        connection.Open();

        SqliteCommand createEmployeesTable = connection.CreateCommand();
        createEmployeesTable.CommandText = @"
        Create Table Employees(
        id Integer PRIMARY KEY AUTOINCREMENT,
        firstName Text NOT NULL,
        lastName Text NOT NULL,
        chip Text NOT NULL)";
        createEmployeesTable.ExecuteNonQuery();

        SqliteCommand createLogsTable = connection.CreateCommand();
        createLogsTable.CommandText = @"
        Create Table Logs(
        id Integer PRIMARY KEY AUTOINCREMENT,
        employeeID Integer NOT NULL REFERENCES Employees(id),
        time Text NOT NULL)";
        createLogsTable.ExecuteNonQuery();

        SqliteCommand createUsersTable = connection.CreateCommand();
        createUsersTable.CommandText = @"
        Create Table Users(
        username Text PRIMARY KEY,
        password Text NOT NULL,
        chip Text NOT NULL)";
        createUsersTable.ExecuteNonQuery();
    }

    private bool checkDatabase() {
        SqliteCommand checkerCommand = connection.CreateCommand();
        checkerCommand.CommandText = "SELECT name from sqlite_master";
        SqliteDataReader checker = checkerCommand.ExecuteReader();

        Dictionary<String, bool> requiredTables = new Dictionary<String, bool>(StringComparer.InvariantCultureIgnoreCase);
        requiredTables.Add("Employees", false);
        requiredTables.Add("Logs", false);
        requiredTables.Add("Users", false);

        while (checker.Read()) {
            String tableName = checker.GetString(0);
            if(requiredTables.ContainsKey(tableName)) requiredTables[tableName] = true;
        }

        foreach (KeyValuePair<String, bool> requiredTable in requiredTables)
            if(!requiredTable.Value) return false;
        return true;
    }

    public DatabaseInterface(String location) {
        String connectionString = new SqliteConnectionStringBuilder {
            DataSource = location,
            Mode = SqliteOpenMode.ReadWrite
        }.ToString();

        connection = new SqliteConnection(connectionString);
        try {
            connection.Open();
        }
        catch (SqliteException ex) {
            if(ex.SqliteErrorCode == 14) { // Unable to open file
                if(!File.Exists(location)) createDatabase();
                else ;//TODO: Add database repair
            }
        }

        if(!checkDatabase()); //TODO: Add database repair
    }

    public String logCheckIn(String chip, DateTime time) {
        SqliteCommand nameGetter = connection.CreateCommand();
        nameGetter.CommandText = "Select id, firstName, lastName from Employees Where chip = $chip";
        nameGetter.Parameters.AddWithValue("$chip", chip);
        SqliteDataReader nameReader = nameGetter.ExecuteReader();

        int id = -1;
        String name = "";
        while(nameReader.Read()) {
            if(id == -1) {
                id = nameReader.GetInt32(0);
                name = nameReader.GetString(1) + " " + nameReader.GetString(2);
            }
            else return $"Vise radnika sa cipom {chip} postoje.";
        }
        if(id == -1) return $"Radnik sa cipom {chip} ne postoji.";

        SqliteCommand stateChecker = connection.CreateCommand();
        stateChecker.CommandText = "Select Count(*) from Logs where employeeID = $id and time < $time and time LIKE $date";
        stateChecker.Parameters.AddWithValue("$id", id);
        stateChecker.Parameters.AddWithValue("$time", $"{time.Year}.{time.Month}.{time.Day}-{time.Hour}:{time.Minute}");
        stateChecker.Parameters.AddWithValue("$date", $"{time.Year}.{time.Month}.{time.Day}%");
        SqliteDataReader stateReader = stateChecker.ExecuteReader();
        stateReader.Read();
        bool isLeaving = stateReader.GetInt32(0) % 2 != 0;

        SqliteCommand logger = connection.CreateCommand();
        logger.CommandText = "Insert into Logs (employeeID, time) Values ($id, $time)";
        logger.Parameters.AddWithValue("$id", id);
        logger.Parameters.AddWithValue("$time", $"{time.Year}.{time.Month}.{time.Day}-{time.Hour}:{time.Minute}");
        logger.ExecuteNonQuery();

        if(isLeaving) return $"{name} je napustio posao u {time.Hour}:{time.Minute}.";
        return $"{name} je dosao na posao u {time.Hour}:{time.Minute}.";
    }

    public bool addEmployee(String firstname, String lastname, String chip, ref String error) {
        if(firstname.Length == 0) {
            error = "Ime ne može da bude prazno.";
            return false;
        }
        if(lastname.Length == 0) {
            error = "Prezime ne može da bude prazno.";
            return false;
        }
        if(chip.Length == 0) {
            error = "Čip ne može da bude prazno.";
            return false;
        }

        SqliteCommand employeeInsertCommand = connection.CreateCommand();
        employeeInsertCommand.CommandText = "Insert into Employees (firstName, lastName, chip) Values ($firstName, $lastName, $chip)";
        employeeInsertCommand.Parameters.AddWithValue("$firstName", firstname);
        employeeInsertCommand.Parameters.AddWithValue("$lastName", lastname);
        employeeInsertCommand.Parameters.AddWithValue("$chip", chip);
        employeeInsertCommand.ExecuteNonQuery();

        return true;
    }

    public bool updateEmployee(int id, String firstname, String lastname, String chip, ref String error) {
        if(firstname.Length == 0) {
            error = "Ime ne može da bude prazno.";
            return false;
        }
        if(lastname.Length == 0) {
            error = "Prezime ne može da bude prazno.";
            return false;
        }
        if(chip.Length == 0) {
            error = "Čip ne može da bude prazno.";
            return false;
        }

        SqliteCommand employeeUpdateCommand = connection.CreateCommand();
        employeeUpdateCommand.CommandText = "Update Employees set firstName = $firstName, lastName = $lastName, chip = $chip where id = $id";
        employeeUpdateCommand.Parameters.AddWithValue("$firstName", firstname);
        employeeUpdateCommand.Parameters.AddWithValue("$lastName", lastname);
        employeeUpdateCommand.Parameters.AddWithValue("$chip", chip);
        employeeUpdateCommand.Parameters.AddWithValue("$id", id);
        if(employeeUpdateCommand.ExecuteNonQuery() == 0) {
            error = "Ne postoji radnik sa datim id-om.";
            return false;
        }
        return true;
    }

    ~DatabaseInterface() {
        connection.Close();
    }
}