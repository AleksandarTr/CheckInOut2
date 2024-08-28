using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CheckInOut2.ViewModels;
using Microsoft.Data.Sqlite;
using MsBox.Avalonia;

namespace CheckInOut2.Models;

public class DatabaseInterface {
    private SqliteConnection connection;

    private const string employeesCreate = @"
        Create Table Employees(
        id Integer PRIMARY KEY AUTOINCREMENT,
        firstName Text NOT NULL,
        lastName Text NOT NULL,
        chip Text NOT NULL)";

    private const string logsCreate = @"
        Create Table Logs(
        id Integer PRIMARY KEY AUTOINCREMENT,
        employeeID Integer NOT NULL REFERENCES Employees(id),
        time Text NOT NULL)";

    private const string usersCreate = @"
        Create Table Users(
        username Text PRIMARY KEY,
        password Text NOT NULL,
        chip Text NOT NULL,
        permission Integer NOT NULL)";

    private void createDatabase() {
        String createConnectionString = new SqliteConnectionStringBuilder(connection.ConnectionString){
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
        
        connection = new SqliteConnection(createConnectionString);
        connection.Open();

        SqliteCommand createEmployeesTable = connection.CreateCommand();
        createEmployeesTable.CommandText = employeesCreate;
        createEmployeesTable.ExecuteNonQuery();

        SqliteCommand createLogsTable = connection.CreateCommand();
        createLogsTable.CommandText = logsCreate;
        createLogsTable.ExecuteNonQuery();

        SqliteCommand createUsersTable = connection.CreateCommand();
        createUsersTable.CommandText = usersCreate;
        createUsersTable.ExecuteNonQuery();
    }

    private void updateDatabase() {
        SqliteCommand addPermissionColumn = connection.CreateCommand();
        addPermissionColumn.CommandText = "Alter table Users add column permission Integer NOT NULL DEFAULT (0)";
        addPermissionColumn.ExecuteNonQuery();
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

        bool result = true;

        foreach (KeyValuePair<String, bool> requiredTable in requiredTables)
            if(!requiredTable.Value) {
                SqliteCommand tableCreationCommand = connection.CreateCommand();
                switch (requiredTable.Key) {
                    case "Employees":
                        tableCreationCommand.CommandText = employeesCreate;
                        break;
                    case "Logs":
                        tableCreationCommand.CommandText = logsCreate;
                        break;
                    case "Users":
                        tableCreationCommand.CommandText = usersCreate;
                        break;
                }
                tableCreationCommand.ExecuteNonQuery();
                result = false;
            }

        SqliteCommand checkUserTable = connection.CreateCommand();
        checkUserTable.CommandText = "Select username, password, chip, permission from Users";
        try {
            checkUserTable.ExecuteNonQuery();
        }
        catch (SqliteException ex) {
            updateDatabase();
        }

        return result;
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
                else return;//TODO: Add database repair
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
        stateChecker.Parameters.AddWithValue("$time", time.ToString("yyyy.MM.dd-HH:mm"));
        stateChecker.Parameters.AddWithValue("$date", time.ToString("yyyy.MM.dd") + "%");
        SqliteDataReader stateReader = stateChecker.ExecuteReader();
        stateReader.Read();
        bool isLeaving = stateReader.GetInt32(0) % 2 != 0;

        SqliteCommand logger = connection.CreateCommand();
        logger.CommandText = "Insert into Logs (employeeID, time) Values ($id, $time)";
        logger.Parameters.AddWithValue("$id", id);
        logger.Parameters.AddWithValue("$time", time.ToString("yyyy.MM.dd-HH:mm"));
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

    public List<String> getActiveEmployees(DateTime date) {
        List<String> employees = new List<String>();

        SqliteCommand employeeFetcherCommand = connection.CreateCommand();
        employeeFetcherCommand.CommandText = @"Select firstName, lastName, Count(*) 
        from Employees E join Logs L on E.id = L.employeeID 
        where time LIKE $date 
        group by firstName, lastName";
        employeeFetcherCommand.Parameters.AddWithValue("$date", date.ToString("yyyy.MM.dd") + "%");
        SqliteDataReader employeeFetcher = employeeFetcherCommand.ExecuteReader();
        
        while(employeeFetcher.Read()) 
            if(employeeFetcher.GetInt32(2) % 2 != 0) 
                employees.Add(employeeFetcher.GetString(0) + " " + employeeFetcher.GetString(1));

        return employees;
    }

    public int checkCertification(string username, string password, string chip) {
        SqliteCommand permissionFetcher = connection.CreateCommand();
        permissionFetcher.CommandText = "Select permission from Users where username = $username and password = $password and chip = $chip";
        permissionFetcher.Parameters.AddWithValue("$username", username);
        permissionFetcher.Parameters.AddWithValue("$password", password);
        permissionFetcher.Parameters.AddWithValue("$chip", chip);

        SqliteDataReader permissionReader = permissionFetcher.ExecuteReader();
        if(permissionReader.Read()) {
            if(username == "admin") return LogInWindowViewModel.MAX_PERMISSION;
            return permissionReader.GetInt32(0);
        }
        return 0;
    }

    public bool addWorker(string firstName, string lastName, string chip, ref string error) {
        SqliteCommand checkNameMatchCommand = connection.CreateCommand();
        checkNameMatchCommand.CommandText = "Select id from Employees where firstName = $firstName and lastName = $lastName";
        checkNameMatchCommand.Parameters.AddWithValue("firstName", firstName);
        checkNameMatchCommand.Parameters.AddWithValue("lastName", lastName);
        if(checkNameMatchCommand.ExecuteReader().Read()) {
            error = $"Već postoji radnik koji se zove {firstName} {lastName}.";
            return false;
        }

        SqliteCommand checkChipCommand = connection.CreateCommand();
        checkChipCommand.CommandText = "Select firstName, lastName from Employees where chip = $chip";
        checkChipCommand.Parameters.AddWithValue("chip", chip);
        SqliteDataReader checkChipReader = checkChipCommand.ExecuteReader();
        if(checkChipReader.Read()) 
            MessageBoxManager.GetMessageBoxStandard("Upozorenje", 
                $"{checkChipReader.GetString(0)} {checkChipReader.GetString(1)} već ima isti broj čipa.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();

        SqliteCommand addCommand = connection.CreateCommand();
        addCommand.CommandText = "Insert into Employees (firstName, lastName, chip) values ($firstName, $lastName, $chip)";
        addCommand.Parameters.AddWithValue("firstName", firstName);
        addCommand.Parameters.AddWithValue("lastName", lastName);
        addCommand.Parameters.AddWithValue("chip", chip);
        if(addCommand.ExecuteNonQuery() == 0) {
            error = "Nije mogao da bude dodat radnik.";
            return false;
        }
        return true;
    }
    
    public bool editWorker(int id, string firstName, string lastName, string chip, ref string error) {
        SqliteCommand checkNameMatchCommand = connection.CreateCommand();
        checkNameMatchCommand.CommandText = "Select id from Employees where firstName = $firstName and lastName = $lastName";
        checkNameMatchCommand.Parameters.AddWithValue("firstName", firstName);
        checkNameMatchCommand.Parameters.AddWithValue("lastName", lastName);
        SqliteDataReader checkNameMatchReader = checkNameMatchCommand.ExecuteReader();
        if(checkNameMatchReader.Read() && (checkNameMatchReader.GetInt32(0) != id || checkNameMatchReader.Read())) {
            error = $"Već postoji radnik koji se zove {firstName} {lastName}.";
            return false;
        }

        SqliteCommand checkChipCommand = connection.CreateCommand();
        checkChipCommand.CommandText = "Select id, firstName, lastName from Employees where chip = $chip";
        checkChipCommand.Parameters.AddWithValue("chip", chip);
        SqliteDataReader checkChipReader = checkChipCommand.ExecuteReader();
        if(checkChipReader.Read() && checkChipReader.GetInt32(0) != id) {
            if(checkChipReader.Read()) MessageBoxManager.GetMessageBoxStandard("Upozorenje", 
                $"{checkChipReader.GetString(1)} {checkChipReader.GetString(2)} već ima isti broj čipa.", 
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
        }

        SqliteCommand workerUpdateCommand = connection.CreateCommand();
        workerUpdateCommand.CommandText = "Update Employees set firstName = $firstName, lastName = $lastName, chip = $chip where id = $id";
        workerUpdateCommand.Parameters.AddWithValue("firstName", firstName);
        workerUpdateCommand.Parameters.AddWithValue("lastName", lastName);
        workerUpdateCommand.Parameters.AddWithValue("chip", chip);
        workerUpdateCommand.Parameters.AddWithValue("id", id);
        if(workerUpdateCommand.ExecuteNonQuery() == 0) {
            error = $"Nije pronađen radnik s id-om {id}";
            return false;
        }

        return true;
    }

    public List<Worker> getWorkers() {
        List<Worker> workers = new List<Worker>();

        SqliteCommand workerFetcherCommand = connection.CreateCommand();
        workerFetcherCommand.CommandText = "Select id, firstName, lastName, chip from Employees";
        SqliteDataReader workerFetcher = workerFetcherCommand.ExecuteReader();
        while(workerFetcher.Read()) 
            workers.Add(new Worker() {
                id = workerFetcher.GetInt32(0),
                firstName = workerFetcher.GetString(1),
                lastName = workerFetcher.GetString(2),
                chip = workerFetcher.GetString(3)
            });

        return workers;
    }    
    
    public List<Check> getChecks(DateTime date, ExportPeriod period = ExportPeriod.Day) {
        List<Check> checks = new List<Check>();
        SqliteCommand checkFetcherCommand = connection.CreateCommand();
        checkFetcherCommand.CommandText = @"Select C.id, time, E.id, firstName, lastName, chip
        from Logs C join Employees E on C.employeeID = E.id
        where time LIKE $date
        order by time ASC";
        _ = period switch
        {
            ExportPeriod.Year => checkFetcherCommand.Parameters.AddWithValue("date", date.ToString("yyyy") + "%"),
            ExportPeriod.Month => checkFetcherCommand.Parameters.AddWithValue("date", date.ToString("yyyy.MM") + "%"),
            ExportPeriod.Day => checkFetcherCommand.Parameters.AddWithValue("date", date.ToString("yyyy.MM.dd") + "%"),
            _ => throw new ArgumentException("Invalid export period provided")
        };
        SqliteDataReader checkFetcher = checkFetcherCommand.ExecuteReader();

        while(checkFetcher.Read()) {
            checks.Add(new Check() {
                id = checkFetcher.GetInt32(0),
                time = DateTime.ParseExact(checkFetcher.GetString(1), "yyyy.MM.dd-HH:mm", CultureInfo.InvariantCulture),
                worker = new Worker() {
                    id = checkFetcher.GetInt32(2),
                    firstName = checkFetcher.GetString(3),
                    lastName = checkFetcher.GetString(4),
                    chip = checkFetcher.GetString(5)
                }
            });
        }
        return checks;
    }

    public bool editCheck(int id, DateTime time) {
        SqliteCommand checkEditorCommand = connection.CreateCommand();
        checkEditorCommand.CommandText = "Update Logs set time = $time where id = $id";
        checkEditorCommand.Parameters.AddWithValue("time", time.ToString("yyyy.MM.dd-HH:mm"));
        checkEditorCommand.Parameters.AddWithValue("id", id);
        if(checkEditorCommand.ExecuteNonQuery() == 0) return false;
        return true;
    }

    public bool deleteCheck(int id) {
        SqliteCommand checkDeleteCommand = connection.CreateCommand();
        checkDeleteCommand.CommandText = "Delete from Logs where id = $id";
        checkDeleteCommand.Parameters.AddWithValue("id", id);
        if(checkDeleteCommand.ExecuteNonQuery() == 0) return false;
        return true;
    }

    ~DatabaseInterface() {
        connection.Close();
    }
}