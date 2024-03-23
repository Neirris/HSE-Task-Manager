using MySql.Data.MySqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "Server=localhost;Uid=root;Pwd=**NepkkaCP3**;";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            DropDatabase(connection, "TaskManager");
            CreateDatabase(connection, "TaskManager");
            connectionString = "Server=localhost;Database=TaskManager;Uid=root;Pwd=**NepkkaCP3**;";
            connection.ChangeDatabase("TaskManager");
            connection.Close();

            connection.Open();

            CreateTable(connection, "Users", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                username VARCHAR(16) NOT NULL, 
                passwordHash VARCHAR(128) NOT NULL,
                token VARCHAR(128) NOT NULL
            ");

            CreateTable(connection, "UsersProfile", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                profilePicture TEXT,
                userID INT,
                FOREIGN KEY (userID) REFERENCES Users(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "Projects", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                title VARCHAR(40) NOT NULL, 
                color VARCHAR(20), 
                userID INT, 
                FOREIGN KEY (userID) REFERENCES Users(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "TaskLists", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                projectID INT, 
                title VARCHAR(40) NOT NULL, 
                userID INT, 
                FOREIGN KEY (projectID) REFERENCES Projects(id) ON DELETE CASCADE, 
                FOREIGN KEY (userID) REFERENCES Users(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "Tasks", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                title VARCHAR(50) NOT NULL, 
                description VARCHAR(1000), 
                status VARCHAR(40) NOT NULL, 
                dateTime DATETIME, 
                isRepeat BOOLEAN, 
                hasNotification BOOLEAN,
                isChecked BOOLEAN,
                taskListID INT, 
                projectID INT, 
                userID INT, 
                FOREIGN KEY (taskListID) REFERENCES TaskLists(id) ON DELETE CASCADE, 
                FOREIGN KEY (projectID) REFERENCES Projects(id) ON DELETE CASCADE, 
                FOREIGN KEY (userID) REFERENCES Users(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "RepeatTasks", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                taskID INT, 
                repeatType VARCHAR(20), 
                repeatDateTime DATETIME, 
                FOREIGN KEY (taskID) REFERENCES Tasks(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "Tags", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                title VARCHAR(20) NOT NULL, 
                color VARCHAR(20),
                userID INT, 
                FOREIGN KEY (userID) REFERENCES Users(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "TagsSet", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                taskID INT, 
                tagID INT, 
                FOREIGN KEY (taskID) REFERENCES Tasks(id) ON DELETE CASCADE, 
                FOREIGN KEY (tagID) REFERENCES Tags(id) ON DELETE CASCADE
            ");

            CreateTable(connection, "Notification", @"
                id INT PRIMARY KEY AUTO_INCREMENT, 
                taskID INT, 
                notificationTime DATETIME, 
                FOREIGN KEY (taskID) REFERENCES Tasks(id) ON DELETE CASCADE
            ");

            CreateAuditTable(connection);
            CreateTaskAuditTriggers(connection);
            CreateUsersProfileTrigger(connection);



            connection.Close();
        }
    }

    static void CreateDatabase(MySqlConnection connection, string databaseName)
    {
        string createDatabaseQuery = $"CREATE DATABASE IF NOT EXISTS {databaseName}";
        MySqlCommand cmd = new MySqlCommand(createDatabaseQuery, connection);
        cmd.ExecuteNonQuery();
    }

    static void DropDatabase(MySqlConnection connection, string databaseName)
    {
        string dropDatabaseQuery = $"DROP DATABASE IF EXISTS `{databaseName}`";
        MySqlCommand cmd = new MySqlCommand(dropDatabaseQuery, connection);
        cmd.ExecuteNonQuery();
    }


    static void CreateTable(MySqlConnection connection, string tableName, string columns)
    {
        string createTableQuery = $"CREATE TABLE IF NOT EXISTS {tableName} ({columns})";
        MySqlCommand cmd = new MySqlCommand(createTableQuery, connection);
        cmd.ExecuteNonQuery();
    }

    static void CreateUsersProfileTrigger(MySqlConnection connection)
    {
        string createTriggerQuery = @"
            CREATE TRIGGER AfterUserInsert
            AFTER INSERT ON Users FOR EACH ROW
            BEGIN
                INSERT INTO UsersProfile (userID, profilePicture) VALUES (NEW.id, NULL);
            END;
        ";
        MySqlCommand cmd = new MySqlCommand(createTriggerQuery, connection);
        cmd.ExecuteNonQuery();
    }

    static void CreateAuditTable(MySqlConnection connection)
    {
        string createAuditTableQuery = @"
        CREATE TABLE IF NOT EXISTS TaskAudit (
            id INT PRIMARY KEY AUTO_INCREMENT,
            taskID INT,
            title VARCHAR(50),
            description VARCHAR(1000),
            status VARCHAR(40),
            dateTime DATETIME,
            isRepeat BOOLEAN,
            hasNotification BOOLEAN,
            isChecked BOOLEAN,
            taskListID INT,
            projectID INT,
            userID INT,
            operationType ENUM('INSERT', 'UPDATE', 'DELETE'),
            changeTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (taskID) REFERENCES Tasks(id) ON DELETE SET NULL
        )";
        MySqlCommand cmd = new MySqlCommand(createAuditTableQuery, connection);
        cmd.ExecuteNonQuery();
    }

    static void CreateTaskAuditTriggers(MySqlConnection connection)
{
    string createInsertTriggerQuery = @"
        CREATE TRIGGER AfterTaskInsert
        AFTER INSERT ON Tasks FOR EACH ROW
        BEGIN
            INSERT INTO TaskAudit (taskID, title, description, status, dateTime, isRepeat, hasNotification, isChecked, taskListID, projectID, userID, operationType)
            VALUES (NEW.id, NEW.title, NEW.description, NEW.status, NEW.dateTime, NEW.isRepeat, NEW.hasNotification, NEW.isChecked, NEW.taskListID, NEW.projectID, NEW.userID, 'INSERT');
        END;
    ";
    MySqlCommand cmdInsert = new MySqlCommand(createInsertTriggerQuery, connection);
    cmdInsert.ExecuteNonQuery();

    string createUpdateTriggerQuery = @"
        CREATE TRIGGER AfterTaskUpdate
        AFTER UPDATE ON Tasks FOR EACH ROW
        BEGIN
            INSERT INTO TaskAudit (taskID, title, description, status, dateTime, isRepeat, hasNotification, isChecked, taskListID, projectID, userID, operationType)
            VALUES (OLD.id, OLD.title, OLD.description, OLD.status, OLD.dateTime, OLD.isRepeat, OLD.hasNotification, OLD.isChecked, OLD.taskListID, OLD.projectID, OLD.userID, 'UPDATE');
        END;
    ";
    MySqlCommand cmdUpdate = new MySqlCommand(createUpdateTriggerQuery, connection);
    cmdUpdate.ExecuteNonQuery();

    string createDeleteTriggerQuery = @"
        CREATE TRIGGER AfterTaskDelete
        AFTER DELETE ON Tasks FOR EACH ROW
        BEGIN
            INSERT INTO TaskAudit (title, description, status, dateTime, isRepeat, hasNotification, isChecked, taskListID, projectID, userID, operationType)
            VALUES (OLD.title, OLD.description, OLD.status, OLD.dateTime, OLD.isRepeat, OLD.hasNotification, OLD.isChecked, OLD.taskListID, OLD.projectID, OLD.userID, 'DELETE');
        END;
    ";
    MySqlCommand cmdDelete = new MySqlCommand(createDeleteTriggerQuery, connection);
    cmdDelete.ExecuteNonQuery();
}


}
