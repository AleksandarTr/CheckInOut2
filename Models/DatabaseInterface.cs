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
        chip Text NOT NULL,
        hourlyRate Real NOT NULL,
        timeConfig Integer NOT NULL
        salary Real NOT NULL)";

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

    private const string timeConfigCreate = @"
        Create Table TimeConfig(
        id Integer NOT NULL,
        day Integer NOT NULL,
        start Text NOT NULL,
        end Text NOT NULL,
        PRIMARY KEY(id, day))";

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

        SqliteCommand createTimeConfigTable = connection.CreateCommand();
        createTimeConfigTable.CommandText = timeConfigCreate;
        createTimeConfigTable.ExecuteNonQuery();

        SqliteCommand addAdmin = connection.CreateCommand();
        string defaultPassword = LogInWindowViewModel.ComputeSha256Hash("password");
        addAdmin.CommandText = "Insert into Users values ('admin', $password, '', 0)";
        addAdmin.Parameters.AddWithValue("password", defaultPassword);
        addAdmin.ExecuteNonQuery();
    }

    private void updateDatabase() {
        try {
            SqliteCommand addPermissionColumn = connection.CreateCommand();
            addPermissionColumn.CommandText = "Alter table Users add column permission Integer NOT NULL DEFAULT (0)";
            addPermissionColumn.ExecuteNonQuery();
        }
        catch(SqliteException) {}

        try {
            SqliteCommand addHourlyRateColumn = connection.CreateCommand();
            addHourlyRateColumn.CommandText = "Alter table Employees add column hourlyRate Real NOT NULL DEFAULT (0)";
            addHourlyRateColumn.ExecuteNonQuery();
        }
        catch(SqliteException) {}

        try {
            SqliteCommand addTimeConfigColumn = connection.CreateCommand();
            addTimeConfigColumn.CommandText = "Alter table Employees add column timeConfig Integer NOT NULL DEFAULT (0)";
            addTimeConfigColumn.ExecuteNonQuery();
        }
        catch(SqliteException) {}

        try {
            SqliteCommand addTimeConfigColumn = connection.CreateCommand();
            addTimeConfigColumn.CommandText = "Alter table Employees add column salary Real NOT NULL DEFAULT (0)";
            addTimeConfigColumn.ExecuteNonQuery();
        }
        catch(SqliteException) {}
    }

    private void updateOldLogs() {
        SqliteCommand oldLogFetcherCommand = connection.CreateCommand();
        oldLogFetcherCommand.CommandText = "select id, time from Logs where time LIKE \"%.____-%\"";
        SqliteDataReader fetcher = oldLogFetcherCommand.ExecuteReader();

        while(fetcher.Read()) {
            int id = fetcher.GetInt32(0);
            string[] dateTime = fetcher.GetString(1).Split('-');
            string[] date = dateTime[0].Split('.');
            string[] time = dateTime[1].Split(':');

            string newTime = $"{int.Parse(date[2]):0000}.{int.Parse(date[1]):00}.{int.Parse(date[0]):00}-{int.Parse(time[0]):00}:{int.Parse(time[1]):00}";
            SqliteCommand oldLogUpdater = connection.CreateCommand();
            oldLogUpdater.CommandText = "Update Logs set time = $time where id = $id";
            oldLogUpdater.Parameters.AddWithValue("id", id);
            oldLogUpdater.Parameters.AddWithValue("time", newTime);
            oldLogUpdater.ExecuteNonQuery();
        }
    }

    private bool checkDatabase() {
        SqliteCommand checkerCommand = connection.CreateCommand();
        checkerCommand.CommandText = "SELECT name from sqlite_master";
        SqliteDataReader checker = checkerCommand.ExecuteReader();

        Dictionary<String, bool> requiredTables = new Dictionary<String, bool>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Employees", false },
            { "Logs", false },
            { "Users", false },
            { "TimeConfig", false }
        };

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
                    case "TimeConfig":
                        tableCreationCommand.CommandText = timeConfigCreate;
                        break;
                }
                tableCreationCommand.ExecuteNonQuery();
                result = false;
            }

        SqliteCommand checkUserTable = connection.CreateCommand();
        checkUserTable.CommandText = "Select username, password, chip, permission from Users";
        try { checkUserTable.ExecuteNonQuery(); }
        catch (SqliteException){ updateDatabase(); }

        SqliteCommand checkEmplyeesTable = connection.CreateCommand();
        checkEmplyeesTable.CommandText = "Select id, firstName, lastName, chip, hourlyRate, timeConfig, salary from Employees";
        try { checkEmplyeesTable.ExecuteNonQuery(); }
        catch (SqliteException){ updateDatabase(); }

        updateOldLogs();
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

        checkDatabase();
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

    public bool addWorker(string firstName, string lastName, string chip, float hourlyRate, int timeConfig, float salary, ref string error) {
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
        addCommand.CommandText = @"Insert into Employees (firstName, lastName, chip, hourlyRate, timeConfig, salary) 
            values ($firstName, $lastName, $chip, $hourlyRate, $timeConfig, $salary)";
        addCommand.Parameters.AddWithValue("firstName", firstName);
        addCommand.Parameters.AddWithValue("lastName", lastName);
        addCommand.Parameters.AddWithValue("chip", chip);
        addCommand.Parameters.AddWithValue("hourlyRate", hourlyRate);
        addCommand.Parameters.AddWithValue("timeConfig", timeConfig);
        addCommand.Parameters.AddWithValue("salary", salary);
        if(addCommand.ExecuteNonQuery() == 0) {
            error = "Nije mogao da bude dodat radnik.";
            return false;
        }
        return true;
    }
    
    public bool editWorker(int id, string firstName, string lastName, string chip, float hourlyRate, int timeConfig, float salary, ref string error) {
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
        workerUpdateCommand.CommandText = @"Update Employees set firstName = $firstName, lastName = $lastName, chip = $chip, 
            hourlyRate = $hourlyRate, timeConfig = $timeConfig, salary = $salary where id = $id";
        workerUpdateCommand.Parameters.AddWithValue("firstName", firstName);
        workerUpdateCommand.Parameters.AddWithValue("lastName", lastName);
        workerUpdateCommand.Parameters.AddWithValue("chip", chip);
        workerUpdateCommand.Parameters.AddWithValue("hourlyRate", hourlyRate);
        workerUpdateCommand.Parameters.AddWithValue("timeConfig", timeConfig);
        workerUpdateCommand.Parameters.AddWithValue("salary", salary);
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
        workerFetcherCommand.CommandText = "Select id, firstName, lastName, chip, hourlyRate, timeConfig, salary from Employees";
        SqliteDataReader workerFetcher = workerFetcherCommand.ExecuteReader();
        while(workerFetcher.Read()) 
            workers.Add(new Worker() {
                id = workerFetcher.GetInt32(0),
                firstName = workerFetcher.GetString(1),
                lastName = workerFetcher.GetString(2),
                chip = workerFetcher.GetString(3),
                hourlyRate = workerFetcher.GetFloat(4),
                timeConfig = workerFetcher.GetInt32(5),
                salary = workerFetcher.GetFloat(6)
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

    public bool addUser(string username, string password, string chip, int permission, ref string error) {
        SqliteCommand checkUsernameCommand = connection.CreateCommand();
        checkUsernameCommand.CommandText = "Select username from Users where username = $username";
        checkUsernameCommand.Parameters.AddWithValue("username", username);
        if(checkUsernameCommand.ExecuteReader().Read()) {
            error = "Već postoji korisnik sa datim korisničkim imenom.";
            return false;
        }

        SqliteCommand userAddCommand = connection.CreateCommand();
        userAddCommand.CommandText = "Insert into Users values ($username, $password, $chip, $permission)";
        userAddCommand.Parameters.AddWithValue("username", username);
        userAddCommand.Parameters.AddWithValue("password", password);
        userAddCommand.Parameters.AddWithValue("chip", chip);
        userAddCommand.Parameters.AddWithValue("permission", permission);
        if(userAddCommand.ExecuteNonQuery() == 0) {
            error = "Nije mogao da bude dodat korisnik.";
            return false;
        }
        return true;
    }

    public List<User> getUsers() {
        List<User> users = new List<User>();
        SqliteCommand userFetcherCommand = connection.CreateCommand();
        userFetcherCommand.CommandText = "Select username, chip, permission from Users";
        SqliteDataReader userFetcher = userFetcherCommand.ExecuteReader();

        while(userFetcher.Read()) {
            users.Add(new User() {
                username = userFetcher.GetString(0),
                chip = userFetcher.GetString(1),
                permission = userFetcher.GetInt32(2)
            });
        }
        return users;
    }

    public bool editUser(string oldUsername, string username, string password, string chip, int permission, ref string error) {
        SqliteCommand usernameCheckCommand = connection.CreateCommand();
        usernameCheckCommand.CommandText = "Select username from Users where username = $username";
        usernameCheckCommand.Parameters.AddWithValue("username", username);
        if(oldUsername != username && usernameCheckCommand.ExecuteReader().Read()) {
            error = "Već postoji korisnik sa istim korisničkim imenom.";
            return false;
        }

        SqliteCommand userEditCommand = connection.CreateCommand();
        userEditCommand.CommandText = "Update Users set username = $username, password = $password, chip = $chip, permission = $permission where username = $oldUsername";
        userEditCommand.Parameters.AddWithValue("username", username);
        userEditCommand.Parameters.AddWithValue("password", password);
        userEditCommand.Parameters.AddWithValue("chip", chip);
        userEditCommand.Parameters.AddWithValue("permission", permission);
        userEditCommand.Parameters.AddWithValue("oldUsername", oldUsername);

        if(userEditCommand.ExecuteNonQuery() == 0) {
            error = "Nije mogao da bude promenjen korisnik.";
            return false;
        }
        return true;
    }

    public List<TimeConfig> GetTimeConfigs() {
        SqliteCommand timeConfigFetcher = connection.CreateCommand();
        timeConfigFetcher.CommandText = @"Select id, day, start, end from TimeConfig order by id ASC, day ASC";
        SqliteDataReader fetcher = timeConfigFetcher.ExecuteReader();

        List<TimeConfig> timeConfigs = new List<TimeConfig>();
        while(fetcher.Read()) {
            string[] start = fetcher.GetString(2).Split(':');
            string[] end = fetcher.GetString(3).Split(':');
            timeConfigs.Add(new TimeConfig() {
                id = fetcher.GetInt32(0),
                day = fetcher.GetInt32(1),
                HourStart = start[0],
                MinuteStart = start[1],
                HourEnd = end[0],
                MinuteEnd = end[1]
            });
        }
        return timeConfigs;
    }

    public TimeConfig? GetTimeConfig(int id, int day) {
        SqliteCommand timeConfigFetcher = connection.CreateCommand();
        timeConfigFetcher.CommandText = "Select start, end from TimeConfig where id = $id and day = $day";
        timeConfigFetcher.Parameters.AddWithValue("id", id);
        timeConfigFetcher.Parameters.AddWithValue("day", day);

        SqliteDataReader fetcher = timeConfigFetcher.ExecuteReader();
        if(fetcher.Read()) {
            string[] start = fetcher.GetString(0).Split(':');
            string[] end = fetcher.GetString(1).Split(':');
            return new TimeConfig() {
                id = id,
                day = day,
                HourStart = start[0],
                MinuteStart = start[1],
                HourEnd = end[0],
                MinuteEnd = end[1]
            };
        }

        return null;
    }

    public int getNextTimeConfigID() {
        SqliteCommand timeConfigIDFetcher = connection.CreateCommand();
        timeConfigIDFetcher.CommandText = "Select COALESCE(max(id), -1) from TimeConfig";
        SqliteDataReader fetcher = timeConfigIDFetcher.ExecuteReader();
        if(fetcher.Read()) return fetcher.GetInt32(0) + 1;
        return 0;
    }

    public void addTimeConfig(TimeConfig timeConfig) {
        SqliteCommand timeConfigAdder = connection.CreateCommand();
        timeConfigAdder.CommandText = "Insert into TimeConfig (id, day, start, end) values ($id, $day, $start, $end)";
        timeConfigAdder.Parameters.AddWithValue("id", timeConfig.id);
        timeConfigAdder.Parameters.AddWithValue("day", timeConfig.day);
        timeConfigAdder.Parameters.AddWithValue("start", $"{int.Parse(timeConfig.HourStart):00}:{int.Parse(timeConfig.MinuteStart):00}");
        timeConfigAdder.Parameters.AddWithValue("end", $"{int.Parse(timeConfig.HourEnd):00}:{int.Parse(timeConfig.MinuteEnd):00}");
        timeConfigAdder.ExecuteNonQuery();
    }

    public void editTimeConfig(TimeConfig timeConfig) {
        SqliteCommand timeConfigEditor = connection.CreateCommand();
        timeConfigEditor.CommandText = "Update TimeConfig set start = $start, end = $end where id = $id and day = $day";
        timeConfigEditor.Parameters.AddWithValue("id", timeConfig.id);
        timeConfigEditor.Parameters.AddWithValue("day", timeConfig.day);
        timeConfigEditor.Parameters.AddWithValue("start", $"{int.Parse(timeConfig.HourStart):00}:{int.Parse(timeConfig.MinuteStart):00}");
        timeConfigEditor.Parameters.AddWithValue("end", $"{int.Parse(timeConfig.HourEnd):00}:{int.Parse(timeConfig.MinuteEnd):00}");
        timeConfigEditor.ExecuteNonQuery();
    }

    public void deleteTimeConfig(TimeConfig timeConfig) {
        SqliteCommand timeConfigRemover = connection.CreateCommand();
        timeConfigRemover.CommandText = "Delete from TimeConfig where id = $id and day = $day";
        timeConfigRemover.Parameters.AddWithValue("id", timeConfig.id);
        timeConfigRemover.Parameters.AddWithValue("day", timeConfig.day);
        timeConfigRemover.ExecuteNonQuery();
    }

    ~DatabaseInterface() {
        connection.Close();
    }
}