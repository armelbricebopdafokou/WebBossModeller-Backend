using WebBossModellerSqlGenerator.Models;
using System.Data.SqlClient;
using System.Data;
using Npgsql;

public class MSSQLDatabaseService : IDatabaseService
{
    /// <summary>
    /// Ruft das Datenbankschema für eine bestimmte Datenbank und ein bestimmtes Schema ab.
    /// </summary>
    /// <param name="connectionString">Die Verbindungszeichenfolge zur Datenbank.</param>
    /// <param name="schemaName">Der Name des Schemas, das abgerufen werden soll.</param>
    /// <returns>Ein <see cref="DbDatabase"/>-Objekt, das das abgerufene Datenbankschema enthält.</returns>
    public DbDatabase GetDatabaseSchema(string connectionString, string schemaName)
    {
        DbDatabase database;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            // Öffnen der Verbindung zur Datenbank
            connection.Open();

            // Initialisieren des DbDatabase-Objekts mit dem Datenbanknamen
            database = new DbDatabase(connection.Database);
            // Erstellen eines neuen DbSchema-Objekts mit dem angegebenen Schemanamen
            DbSchema schema = new DbSchema(schemaName);

            // Laden der Tabellen für das angegebene Schema und Hinzufügen zum Schema
            schema.Tables.AddRange(LoadTables(connection, schema.Name));

            // Hinzufügen des Schemas zur Datenbank
            database.Schemas.Add(schema);
        }

        // Rückgabe des DbDatabase-Objekts, das das abgerufene Schema enthält
        return database;
    }

    /// <summary>
    /// Lädt die Tabellen eines bestimmten Schemas aus der Datenbank.
    /// </summary>
    /// <param name="connection">Die SQL-Verbindung, die verwendet wird, um die Tabelleninformationen abzurufen.</param>
    /// <param name="schemaName">Der Name des Schemas, aus dem die Tabellen geladen werden sollen.</param>
    /// <returns>Eine Liste von <see cref="DbTable"/>-Objekten, die die Tabellen des Schemas repräsentieren.</returns>
    private List<DbTable> LoadTables(SqlConnection connection, string schemaName)
    {
        List<DbTable> tables = new List<DbTable>();

        // Ruft die Tabelleninformationen des angegebenen Schemas ab
        DataTable tablesTable = connection.GetSchema("Tables", new string[] { null, schemaName });

        // Durchläuft jede Zeile in der DataTable, die die Tabelleninformationen enthält
        foreach (DataRow tableRow in tablesTable.Rows)
        {
            // Erstellt ein neues DbTable-Objekt und füllt es mit den Tabelleninformationen
            DbTable table = new DbTable
            {
                /// <summary>
                /// Der Name der Tabelle.
                /// </summary>
                Name = tableRow["TABLE_NAME"].ToString()!
            };

            // Lädt die Spalten der Tabelle und fügt sie zur Tabelle hinzu
            table.Columns.AddRange(LoadColumns(connection, schemaName, table.Name));

            // Identifiziert Primär- und Fremdschlüssel und markiert sie in den Spalten
            AddConstraints(connection, schemaName, table.Name, table.Columns);

            // Ermittelt eindeutige Kombinationen von Spalten und speichert sie in der Tabelle
            table.UniqueCombination = GetUniqueCombination(connection, schemaName, table.Name, table.Columns);

            // Überprüft, ob die Tabelle eine schwache Entität ist
            table.IsWeak = IsWeakEntity(connection, schemaName, table.Name);

            // Fügt die erstellte Tabelle zur Liste hinzu
            tables.Add(table);
        }

        // Gibt die Liste der Tabellen zurück
        return tables;
    }

    /// <summary>
    /// Lädt die Spalten einer bestimmten Tabelle aus der Datenbank.
    /// </summary>
    /// <param name="connection">Die SQL-Verbindung, die verwendet wird, um die Spalteninformationen abzurufen.</param>
    /// <param name="schemaName">Der Name des Schemas, in dem sich die Tabelle befindet.</param>
    /// <param name="tableName">Der Name der Tabelle, deren Spalten geladen werden sollen.</param>
    /// <returns>Eine Liste von <see cref="DbColumn"/>-Objekten, die die Spalten der Tabelle repräsentieren.</returns>
    private List<DbColumn> LoadColumns(SqlConnection connection, string schemaName, string tableName)
    {
        List<DbColumn> columns = new List<DbColumn>();

        // Ruft die Spalteninformationen der angegebenen Tabelle ab
        DataTable columnsTable = connection.GetSchema("Columns", new string[] { null, schemaName, tableName });

        // Durchläuft jede Zeile in der DataTable, die die Spalteninformationen enthält
        foreach (DataRow columnRow in columnsTable.Rows)
        {
            // Erstellt ein neues DbColumn-Objekt und füllt es mit den Spalteninformationen
            DbColumn column = new DbColumn
            {
                /// <summary>
                /// Der Name der Spalte.
                /// </summary>
                Name = columnRow["COLUMN_NAME"].ToString()!,

                /// <summary>
                /// Der Datentyp der Spalte.
                /// </summary>
                Type = columnRow["DATA_TYPE"].ToString()!,

                /// <summary>
                /// Gibt an, ob die Spalte NULL-Werte zulässt.
                /// </summary>
                IsNull = columnRow["IS_NULLABLE"].ToString() == "YES",

                /// <summary>
                /// Der Standardwert der Spalte, falls vorhanden.
                /// </summary>
                DefaultValue = columnRow["COLUMN_DEFAULT"]?.ToString() ?? string.Empty
            };

            // Fügt die erstellte Spalte zur Liste hinzu
            columns.Add(column);
        }

        // Gibt die Liste der Spalten zurück
        return columns;
    }


    /// <summary>
    /// Adds constraints to all columns of the table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The list of columns to which constraints will be added.</param>
    private void AddConstraints(SqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Iteration durch jede Spalte in der Liste der Spalten
        foreach (DbColumn col in columns)
        {
            // SQL-Abfrage, um die Constraints der Spalten abzurufen
            string uniqueColumnsQuery = @"
                            SELECT 
                                c.name AS ColumnName,
                                kc.name AS ConstraintName,
                                kc.type AS ConstraintType,
                                NULL AS ConstraintDefinition,
                                NULL AS ReferencedTable,
                                NULL AS ReferencedColumn
                            FROM sys.key_constraints kc
                            JOIN sys.index_columns ic 
                                ON kc.parent_object_id = ic.object_id 
                                AND kc.unique_index_id = ic.index_id
                            JOIN sys.columns c 
                                ON ic.object_id = c.object_id 
                                AND ic.column_id = c.column_id
                            JOIN sys.tables t 
                                ON kc.parent_object_id = t.object_id
                            JOIN sys.schemas s 
                                ON t.schema_id = s.schema_id
                            WHERE s.name = @schema 
                            AND t.name = @table
                            AND c.name = @col

                            UNION ALL

                            SELECT
                                cp.name AS ColumnName,
                                fk.name AS ConstraintName,
                                'FK' AS ConstraintType,
                                NULL AS ConstraintDefinition,
                                tr.name AS ReferencedTable,
                                cr.name AS ReferencedColumn
                            FROM sys.foreign_keys fk
                            JOIN sys.foreign_key_columns fkc 
                                ON fk.object_id = fkc.constraint_object_id
                            JOIN sys.tables tp 
                                ON tp.object_id = fkc.parent_object_id
                            JOIN sys.schemas s 
                                ON tp.schema_id = s.schema_id
                            JOIN sys.columns cp 
                                ON fkc.parent_column_id = cp.column_id 
                                AND fkc.parent_object_id = cp.object_id
                            JOIN sys.tables tr 
                                ON tr.object_id = fkc.referenced_object_id
                            JOIN sys.schemas sr 
                                ON tr.schema_id = sr.schema_id
                            JOIN sys.columns cr 
                                ON fkc.referenced_column_id = cr.column_id 
                                AND fkc.referenced_object_id = cr.object_id
                            WHERE s.name = @schema 
                            AND tp.name = @table
                            AND cp.name = @col

                            UNION ALL

                            SELECT 
                               c.name AS ColumnName,
                                cc.name AS ConstraintName,
                                'C' AS ConstraintType,
                                cc.definition AS ConstraintDefinition,
                                NULL AS ReferencedTable,
                                NULL AS ReferencedColumn
                            FROM sys.check_constraints cc  
                            JOIN sys.columns c 
                                ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
                            JOIN sys.tables t 
                                ON cc.parent_object_id = t.object_id 
                            JOIN sys.schemas s 
                                ON t.schema_id = s.schema_id
                            WHERE s.name = @schema 
                            AND t.name = @table
                            AND c.name = @col

                            UNION ALL

                            SELECT
                                c.name AS ColumnName,
                                d.name AS ConstraintName,
                                'D' AS ConstraintType,
                                d.definition AS ConstraintDefinition,
                                NULL AS ReferencedTable,
                                NULL AS ReferencedColumn
                            FROM sys.default_constraints d
                            JOIN sys.columns c 
                                ON d.parent_column_id = c.column_id 
                                AND d.parent_object_id = c.object_id
                            JOIN sys.tables t 
                                ON d.parent_object_id = t.object_id
                            JOIN sys.schemas s 
                                ON t.schema_id = s.schema_id
                            WHERE s.name = @schema 
                            AND t.name = @table
                            AND c.name = @col;";

            // Erstellen des SQL-Befehls
            using (var command = new SqlCommand(uniqueColumnsQuery, connection))
            {
                // Hinzufügen der Parameter zur SQL-Abfrage
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@col", col.Name);
                command.Parameters.AddWithValue("@schema", schemaName);

                // Ausführen des SQL-Befehls und Verarbeiten der Ergebnismenge
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Überprüfen des Typs des Constraints und Setzen der entsprechenden Eigenschaften der Spalte
                        switch (reader["ConstraintType"].ToString())
                        {
                            case "PK":
                                col.IsPrimaryKey = true;
                                col.IsNull = false;
                                break;
                            case "UQ":
                                col.IsUnique = true;
                                break;
                            case "FK":
                                col.IsForeignKey = true;
                                col.ReferenceColumn = reader["ReferencedColumn"].ToString();
                                col.ReferenceTable = new DbTable() { Name = reader["ReferencedTable"].ToString() };
                                break;
                            case "C":
                                col.CheckConstraint = reader["ConstraintDefinition"].ToString();
                                break;
                            //case "D":
                            //    col.DefaultValue = reader["check_definition"].ToString();
                            //    break;
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
    public HashSet<List<DbColumn>> GetUniqueCombination(SqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Erstellen einer neuen Menge, um die eindeutigen Spaltenkombinationen zu speichern
        HashSet<List<DbColumn>> uniqueCombinationSet = new HashSet<List<DbColumn>>();

        // SQL-Abfrage, um die eindeutigen Spaltenkombinationen abzurufen
        string sql = @"SELECT 
                        kc.name AS constraint_name,
                        'UQ' AS constraint_type, -- 'UQ' represents unique constraints
                        STRING_AGG(c.name, ', ') AS column_names,
                        t.name AS table_name,
                        s.name AS schema_name
                    FROM 
                        sys.key_constraints kc
                    JOIN 
                        sys.tables t ON kc.parent_object_id = t.object_id
                    JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    JOIN 
                        sys.index_columns ic ON kc.unique_index_id = ic.index_id AND kc.parent_object_id = ic.object_id
                    JOIN 
                        sys.columns c ON ic.column_id = c.column_id AND ic.object_id = c.object_id
                    WHERE 
                        s.name = @schemaName
                        AND t.name = @tableName
                        AND kc.type = 'UQ' -- Filter for unique constraints
                    GROUP BY 
                        kc.name, t.name, s.name
                    HAVING 
                        COUNT(ic.column_id) > 1; -- Filter for composite constraints (more than one column)
                ";

        // Erstellen des SQL-Befehls
        using (var command = new SqlCommand(sql, connection))
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
                    var cols = reader.GetFieldValue<string>(2).Split(",").Select(x => x.Trim()).ToArray(); ;
                    if (cols.Length > 1)
                    {
                        List<DbColumn> uniqueCombinations = new List<DbColumn>();
                        foreach (var column in columns)
                        {
                            //todo: delete space in the column_name in cols
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
    /// Überprüft, ob eine Tabelle eine schwache Entität ist.
    /// Eine schwache Entität ist eine Tabelle, deren Primärschlüssel Fremdschlüssel enthält.
    /// </summary>
    /// <param name="connection">Die SQL-Verbindung zur Datenbank.</param>
    /// <param name="schemaName">Der Name des Schemas, in dem sich die Tabelle befindet.</param>
    /// <param name="tableName">Der Name der Tabelle, die überprüft werden soll.</param>
    /// <returns>
    /// <c>true</c>, wenn die Tabelle eine schwache Entität ist (d. h. der Primärschlüssel Fremdschlüssel enthält);
    /// andernfalls <c>false</c>.
    /// </returns>
    private bool IsWeakEntity(SqlConnection connection, string schemaName, string tableName)
    {
        // SQL-Abfrage, um die Spalten und Constraint-Typen (Primärschlüssel und Fremdschlüssel) der Tabelle abzurufen
        string query = @"
                    SELECT 
                    c.name AS ColumnName,
                    'FK' AS ConstraintType -- Explicitly set ConstraintType to 'FK' for foreign keys
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc 
                    ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.columns c 
                    ON fkc.parent_object_id = c.object_id 
                    AND fkc.parent_column_id = c.column_id
                WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = @SchemaName
                  AND OBJECT_NAME(fk.parent_object_id) = @TableName
                UNION ALL
                SELECT 
                    c.name AS ColumnName,
                    kc.type AS ConstraintType -- Retrieve PK or UQ from key_constraints
                FROM sys.key_constraints kc
                INNER JOIN sys.index_columns ic 
                    ON kc.unique_index_id = ic.index_id 
                    AND kc.parent_object_id = ic.object_id
                INNER JOIN sys.columns c 
                    ON ic.column_id = c.column_id 
                    AND ic.object_id = c.object_id
                WHERE OBJECT_SCHEMA_NAME(kc.parent_object_id) = @SchemaName
                  AND OBJECT_NAME(kc.parent_object_id) = @TableName;";

        // HashSet, um die Spalten des Primärschlüssels zu speichern
        HashSet<string> primaryKeyColumns = new HashSet<string>();

        // HashSet, um die Spalten des Fremdschlüssels zu speichern
        HashSet<string> foreignKeyColumns = new HashSet<string>();

        // SQL-Befehl erstellen und ausführen
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            // Parameter für Schema- und Tabellennamen hinzufügen
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            // SQL-Abfrage ausführen und Ergebnisse lesen
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Spaltenname und Constraint-Typ auslesen
                    var columnName = reader["ColumnName"].ToString();
                    var constraintType = reader["ConstraintType"].ToString();

                    // Wenn der Constraint ein Primärschlüssel ist, Spalte zum HashSet hinzufügen
                    if (constraintType == "PK")
                    {
                        primaryKeyColumns.Add(columnName);

                        if(reader["ColumnName"] != DBNull.Value && foreignKeyColumns.Contains(columnName))
                        {
                            return true;
                        }
                    }

                    // Wenn der Constraint ein Fremdschlüssel ist und die Spalte Teil des Primärschlüssels ist,
                    // das Flag auf true setzen
                    if (constraintType == "FK")
                    {
                        foreignKeyColumns.Add(columnName);

                        if (reader["ColumnName"] != DBNull.Value && primaryKeyColumns.Contains(columnName))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // Eine Tabelle ist eine schwache Entität, wenn:
        // - Sie einen Primärschlüssel hat, der Fremdschlüssel enthält.
        // - Sie keinen starken eigenen Primärschlüssel hat.
        return false;
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

        // SQL query to get all schemas in the current database
        string query = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA;";

        // Create a connection to the database
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            try
            {
                // Open the connection
                connection.Open();

                // Create a command to execute the query
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Execute the query and get the results
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine($"Schemas in the database '{connection.Database}':");
                        while (reader.Read())
                        {
                            // Read the schema name from the result set
                            SchemaList.Add(reader["SCHEMA_NAME"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               throw new Exception(ex.Message );
            }
        }

        // Rückgabe der Liste der Schemanamen
        return SchemaList;
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
        using (var conn = new SqlConnection(connectionString))
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
    /// Führt ein SQL-Skript aus.
    /// </summary>
    /// <param name="connectionString">Die Verbindungszeichenfolge zur Datenbank.</param>
    /// <param name="sqlScript">Das auszuführende SQL-Skript.</param>
    /// <returns>Gibt true zurück, wenn das Skript erfolgreich ausgeführt wurde, andernfalls false.</returns>
    public void ExecuteScript(string connectionString, string sqlScript)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlCommand command = new SqlCommand(sqlScript, conn))
            {
                command.ExecuteNonQuery();
            }
            conn.Close();
        }

    }

}