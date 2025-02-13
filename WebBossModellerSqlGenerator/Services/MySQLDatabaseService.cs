using System.Data;
using MySql.Data.MySqlClient;
using WebBossModellerSqlGenerator.Models;

public class MySQLDatabaseService : IDatabaseService
{
    /// <summary>
    /// Create a complete database
    /// </summary>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <returns>A DbDatabase object representing the database schema.</returns>
    public DbDatabase GetDatabaseSchema(string connectionString, string schemaName)
    {
        // Initialisieren der Datenbankvariable
        DbDatabase database = null;

        // Erstellen einer neuen Verbindung mit der übergebenen Verbindungszeichenfolge
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            // Öffnen der Verbindung zur Datenbank
            connection.Open();

            // Initialisieren des DbDatabase-Objekts mit dem Datenbanknamen
            database = new DbDatabase(connection.Database);

            // Erstellen eines neuen DbSchema-Objekts mit dem angegebenen Schemanamen
            DbSchema schema = new DbSchema(schemaName);

            // Hinzufügen der Tabellen zum Schema
            schema.Tables.AddRange(LoadTables(connection, schema.Name));

            // Hinzufügen des Schemas zur Datenbank
            database.Schemas.Add(schema);
        }

        // Rückgabe des DbDatabase-Objekts
        return database;
    }


    /// <summary>
    /// Loads the tables of a specified schema from the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <returns>A list of DbTable objects representing the tables in the schema.</returns>
    private List<DbTable> LoadTables(MySqlConnection connection, string schemaName)
    {
        // Erstellen einer neuen Liste, um die Tabellen des Schemas zu speichern
        List<DbTable> tables = new List<DbTable>();

        // Abrufen der Tabelleninformationen des angegebenen Schemas aus der Datenbank
        var tablesTable = connection.GetSchema("Tables", new string[] { null, schemaName });
        foreach (DataRow tableRow in tablesTable.Rows)
        {
            // Erstellen eines neuen DbTable-Objekts und Setzen der entsprechenden Eigenschaften
            DbTable table = new DbTable
            {
                Name = tableRow["table_name"].ToString()!
            };

            // Hinzufügen der Spalten zur Tabelle
            table.Columns.AddRange(LoadColumns(connection, schemaName, table.Name));

            // Markieren von Primär- und Fremdschlüsseln
            AddConstraints(connection, schemaName, table.Name, table.Columns);

            // Hinzufügen eindeutiger Kombinationen von Spalten
            table.UniqueCombination = GetUniqueCombination(connection, schemaName, table.Name, table.Columns);

            // Überprüfen, ob die Tabelle eine schwache Entität ist
            table.IsWeak = IsWeakEntity(connection, schemaName, table.Name);

            // Hinzufügen der Tabelle zur Liste der Tabellen
            tables.Add(table);
        }

        // Rückgabe der Liste der Tabellen
        return tables;
    }


    /// <summary>
    /// Loads the columns of a specified table from the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>A list of DbColumn objects representing the columns of the table.</returns>
    private List<DbColumn> LoadColumns(MySqlConnection connection, string schemaName, string tableName)
    {
        // Erstellen einer neuen Liste, um die Spalten der Tabelle zu speichern
        List<DbColumn> columns = new List<DbColumn>();

        // Abrufen der Spalteninformationen der angegebenen Tabelle aus der Datenbank
        var columnsTable = connection.GetSchema("Columns", new string[] { null, schemaName, tableName });
        foreach (DataRow columnRow in columnsTable.Rows)
        {
            // Erstellen eines neuen DbColumn-Objekts und Setzen der entsprechenden Eigenschaften
            DbColumn column = new DbColumn
            {
                Name = columnRow["column_name"].ToString()!,
                Type = columnRow["data_type"].ToString()!,
                IsNull = columnRow["is_nullable"].ToString() == "YES",
                DefaultValue = columnRow["COLUMN_DEFAULT"]?.ToString() ?? string.Empty
            };

            // Hinzufügen der Spalte zur Liste
            columns.Add(column);
        }

        // Rückgabe der Liste der Spalten
        return columns;
    }


    /// <summary>
    /// Determines if the specified table is a weak entity based on the presence of foreign keys in its primary key.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>True if the table is a weak entity, otherwise false.</returns>
    private bool IsWeakEntity(MySqlConnection connection, string schemaName, string tableName)
    {
        // SQL-Abfrage, um die Constraints der Spalten abzurufen
        string query = @"
        SELECT 
            tc.CONSTRAINT_NAME AS constraint_name,
            tc.CONSTRAINT_TYPE AS constraint_type,
            kcu.COLUMN_NAME AS column_name,
            kcu.REFERENCED_TABLE_NAME AS referenced_table,
            kcu.REFERENCED_COLUMN_NAME AS referenced_column
        FROM 
            information_schema.TABLE_CONSTRAINTS tc
        LEFT JOIN 
            information_schema.KEY_COLUMN_USAGE kcu
            ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
            AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            AND tc.TABLE_NAME = kcu.TABLE_NAME
        WHERE 
            tc.TABLE_SCHEMA = @SchemaName
            AND tc.TABLE_NAME = @TableName;        
            ";

        // Erstellen einer neuen Menge, um die Primärschlüsselspalten zu speichern
        HashSet<string> primaryKeyColumns = new HashSet<string>();

        // Variable zur Überprüfung, ob ein Fremdschlüssel im Primärschlüssel vorhanden ist
        bool hasForeignKeyInPrimaryKey = false;

        // Erstellen des SQL-Befehls
        using (var command = new MySqlCommand(query, connection))
        {
            // Hinzufügen der Parameter zur SQL-Abfrage
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            // Ausführen des SQL-Befehls und Verarbeiten der Ergebnismenge
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Abrufen des Constraint-Typs und des Spaltennamens aus der Ergebnismenge
                    var constraintType = reader["constraint_type"].ToString();
                    var columnName = reader["column_name"].ToString();
                    if (constraintType == "PRIMARY KEY")
                    {
                        // Hinzufügen der Primärschlüsselspalte zur Menge
                        primaryKeyColumns.Add(columnName);
                    }

                    if (constraintType == "FOREIGN KEY" && primaryKeyColumns.Contains(columnName))
                    {
                        // Überprüfen, ob ein Fremdschlüssel im Primärschlüssel vorhanden ist
                        hasForeignKeyInPrimaryKey = true;
                    }
                }
            }
        }

        // Rückgabe des Ergebnisses, ob die Tabelle eine schwache Entität ist
        return hasForeignKeyInPrimaryKey;
    }



    /// <summary>
    /// Adds constraints to all columns of the table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The list of columns to which constraints will be added.</param>
    private void AddConstraints(MySqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Iteration durch jede Spalte in der Liste der Spalten
        foreach (DbColumn col in columns)
        {
            // SQL-Abfrage, um die Constraints der Spalten abzurufen
            string uniqueColumnsQuery = @"
                            -- Primary Keys
                        SELECT 
                            tc.CONSTRAINT_NAME AS constraint_name,
                            'PRIMARY KEY' AS constraint_type,
                            kcu.COLUMN_NAME AS column_name,
                            tc.TABLE_NAME AS table_name,
                            tc.TABLE_SCHEMA AS schema_name,
                            NULL AS check_definition,
                            NULL AS referenced_table,
                            NULL AS referenced_column
                        FROM 
                            information_schema.TABLE_CONSTRAINTS tc
                        JOIN 
                            information_schema.KEY_COLUMN_USAGE kcu
                            ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                            AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                            AND tc.TABLE_NAME = kcu.TABLE_NAME
                        WHERE 
                            tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                            AND tc.TABLE_SCHEMA = @schema
                            AND tc.TABLE_NAME = @table
                            AND kcu.COLUMN_NAME = @column

                        UNION ALL

                        -- Foreign Keys
                        SELECT 
                            tc.CONSTRAINT_NAME AS constraint_name,
                            'FOREIGN KEY' AS constraint_type,
                            kcu.COLUMN_NAME AS column_name,
                            tc.TABLE_NAME AS table_name,
                            tc.TABLE_SCHEMA AS schema_name,
                            NULL AS check_definition,
                            kcu.REFERENCED_TABLE_NAME AS referenced_table,
                            kcu.REFERENCED_COLUMN_NAME AS referenced_column
                        FROM 
                            information_schema.TABLE_CONSTRAINTS tc
                        JOIN 
                            information_schema.KEY_COLUMN_USAGE kcu
                            ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                            AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                            AND tc.TABLE_NAME = kcu.TABLE_NAME
                        WHERE 
                            tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
                            AND tc.TABLE_SCHEMA = @schema
                            AND tc.TABLE_NAME = @table
                            AND kcu.COLUMN_NAME = @column

                        UNION ALL

                        -- Unique Constraints
                        SELECT 
                            tc.CONSTRAINT_NAME AS constraint_name,
                            'UNIQUE' AS constraint_type,
                            kcu.COLUMN_NAME AS column_name,
                            tc.TABLE_NAME AS table_name,
                            tc.TABLE_SCHEMA AS schema_name,
                            NULL AS check_definition,
                            NULL AS referenced_table,
                            NULL AS referenced_column
                        FROM 
                            information_schema.TABLE_CONSTRAINTS tc
                        JOIN 
                            information_schema.KEY_COLUMN_USAGE kcu
                            ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                            AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                            AND tc.TABLE_NAME = kcu.TABLE_NAME
                        WHERE 
                            tc.CONSTRAINT_TYPE = 'UNIQUE'
                            AND tc.TABLE_SCHEMA = @schema
                            AND tc.TABLE_NAME = @table
                            AND kcu.COLUMN_NAME = @column

                        UNION ALL

                        -- Check Constraints (MySQL 8.0.16+)
                        SELECT 
                            cc.CONSTRAINT_NAME AS constraint_name,
                            'CHECK' AS constraint_type,
                            NULL AS column_name,
                            cc.TABLE_NAME AS table_name,
                            cc.CONSTRAINT_SCHEMA AS schema_name,
                            cc.CHECK_CLAUSE AS check_definition,
                            NULL AS referenced_table,
                            NULL AS referenced_column
                        FROM 
                            information_schema.CHECK_CONSTRAINTS cc
                        WHERE 
                            cc.TABLE_SCHEMA = @schema
                            AND cc.TABLE_NAME = @table
                            AND EXISTS (
                                SELECT 1
                                FROM information_schema.CHECK_CONSTRAINTS cc2
                                WHERE cc2.CONSTRAINT_SCHEMA = cc.CONSTRAINT_SCHEMA
                                AND cc2.TABLE_NAME = cc.TABLE_NAME
                                AND cc2.CHECK_CLAUSE LIKE CONCAT('%', @column, '%')
                            );";

            // Erstellen des SQL-Befehls
            using (var command = new MySqlCommand(uniqueColumnsQuery, connection))
            {
                // Hinzufügen der Parameter zur SQL-Abfrage
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@column", col.Name);
                command.Parameters.AddWithValue("@schema", schemaName);

                // Ausführen des SQL-Befehls und Verarbeiten der Ergebnismenge
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Überprüfen des Typs des Constraints und Setzen der entsprechenden Eigenschaften der Spalte
                        switch (reader["constraint_type"].ToString())
                        {
                            case "PRIMARY KEY":
                                col.IsPrimaryKey = true;
                                col.IsNull = false;
                                break;
                            case "UNIQUE":
                                col.IsUnique = true;
                                break;
                            case "FOREIGN KEY":
                                
                                col.IsForeignKey = true;
                                col.ReferenceColumn = reader["referenced_column"].ToString();
                                col.ReferenceTable = new DbTable() { Name = reader["referenced_table"].ToString() };
                                break;
                            case "CHECK":
                                col.CheckConstraint = reader["check_definition"].ToString();
                                break;
                        }
                    }
                }
            }
        }
    }



    /// <summary>
    /// Retrieves unique column combinations from a specified table in the database.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The list of columns to check for unique combinations.</param>
    /// <returns>A HashSet of lists of DbColumns representing unique combinations.</returns>
    public HashSet<List<DbColumn>> GetUniqueCombination(MySqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Erstellen einer neuen Menge, um die eindeutigen Spaltenkombinationen zu speichern
        HashSet<List<DbColumn>> uniqueCombinationSet = new HashSet<List<DbColumn>>();

        // SQL-Abfrage, um die eindeutigen Spaltenkombinationen abzurufen
        string sql = @"SELECT 
                        tc.CONSTRAINT_NAME AS constraint_name,
                        'UNIQUE' AS constraint_type,
                        GROUP_CONCAT(kcu.COLUMN_NAME ORDER BY kcu.ORDINAL_POSITION) AS column_names,
                        tc.TABLE_NAME AS table_name,
                        tc.TABLE_SCHEMA AS schema_name
                    FROM 
                        information_schema.TABLE_CONSTRAINTS tc
                    JOIN 
                        information_schema.KEY_COLUMN_USAGE kcu
                        ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                        AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                        AND tc.TABLE_NAME = kcu.TABLE_NAME
                    WHERE 
                        tc.CONSTRAINT_TYPE = 'UNIQUE'  -- Only unique constraints
                        AND tc.TABLE_SCHEMA = @schemaName  -- Schema name
                        AND tc.TABLE_NAME = @tableName  -- Table name
                    GROUP BY 
                        tc.CONSTRAINT_NAME, tc.TABLE_NAME, tc.TABLE_SCHEMA
                    HAVING 
                        COUNT(kcu.COLUMN_NAME) > 1;  -- Only composite constraints (more than one column)
                ";

        // Erstellen des SQL-Befehls
        using (var command = new MySqlCommand(sql, connection))
        {
            // Hinzufügen der Parameter zur SQL-Abfrage
            command.Parameters.AddWithValue("@schemaName", schemaName);
            command.Parameters.AddWithValue("@tableName", tableName);

            // Ausführen des SQL-Befehls und Verarbeiten der Ergebnismenge
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Abrufen der Spaltennamen aus der Ergebnismenge
                    var cols = reader.GetFieldValue<string[]>(2);
                    if (cols.Length > 1)
                    {
                        List<DbColumn> uniqueCombinations = new List<DbColumn>();
                        foreach (var column in columns)
                        {
                            if (cols.Contains(column.Name))
                            {
                                uniqueCombinations.Add(column);
                            }
                        }
                        uniqueCombinationSet.Add(uniqueCombinations);
                    }
                }
            }
            return uniqueCombinationSet;
        }
    }



    /// <summary>
    /// Retrieves a list of database names from the specified connection.
    /// </summary>
    /// <param name="connectionString">The connection string to the server.</param>
    /// <returns>A list of database names.</returns>
    public List<string> GetDatabaseList(string connectionString)
    {
        // Erstellen einer neuen Liste, um die Datenbanknamen zu speichern
        List<string> databaseList = new List<string>();

        // Erstellen einer neuen Verbindung mit der übergebenen Verbindungszeichenfolge
        using (var conn = new MySqlConnection(connectionString))
        {
            // Öffnen der Verbindung zum Server
            conn.Open();

            // Abrufen aller Datenbanken vom Server
            DataTable databases = conn.GetSchema("Databases");

            // Iteration durch jede Zeile in der abgerufenen Ergebnistabelle
            foreach (DataRow row in databases.Rows)
            {
                // Hinzufügen des Datenbanknamens zur Liste
                databaseList.Add(row["database_name"].ToString());
            }
        }

        // Rückgabe der Liste der Datenbanknamen
        return databaseList;
    }


    /// <summary>
    /// Retrieves a list of schema names from the specified database.
    /// </summary>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="databaseName">The name of the database (not used in this function).</param>
    /// <returns>A list of schema names.</returns>
    public List<string> GetSchemaListFromDatabase(string connectionString, string databaseName)
    {
        // Erstellen einer neuen Liste, um die Schemanamen zu speichern
        List<string> SchemaList = new List<string>();

        // Erstellen einer neuen Verbindung mit der übergebenen Verbindungszeichenfolge
        using (var conn = new MySqlConnection(connectionString))
        {
            // Öffnen der Verbindung zur Datenbank
            conn.Open();

            // Abrufen aller Schemata aus der Datenbank
            DataTable dt = conn.GetSchema("Schemata");

            // Iteration durch jede Zeile in der abgerufenen Ergebnistabelle
            foreach (DataRow row in dt.Rows)
            {
                // Hinzufügen des Schemanamens zur Liste
                SchemaList.Add(row["schema_name"].ToString());
            }
        }

        // Rückgabe der Liste der Schemanamen
        return SchemaList;
    }



}