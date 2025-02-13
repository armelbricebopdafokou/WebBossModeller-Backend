using System.Data;
using Mysqlx.Resultset;
using Npgsql;
using WebBossModellerSqlGenerator.Models;

public class PostgreSQLDatabaseService : IDatabaseService
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
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
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
    private List<DbTable> LoadTables(NpgsqlConnection connection, string schemaName)
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
    private List<DbColumn> LoadColumns(NpgsqlConnection connection, string schemaName, string tableName)
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
    private bool IsWeakEntity(NpgsqlConnection connection, string schemaName, string tableName)
    {
        // SQL-Abfrage, um die Constraints der Spalten abzurufen
        string query = @"
        SELECT
                con.conname AS constraint_name,
                con.contype AS constraint_type,
                CASE
                    WHEN con.contype = 'p' THEN a.attname -- Primary Key column
                    WHEN con.contype = 'f' THEN a.attname -- Foreign Key column
                    WHEN con.contype = 'u' THEN a.attname -- Unique Constraint column
                    WHEN con.contype = 'c' THEN NULL      -- Check constraints don't map to a specific column
                    ELSE NULL
                END AS column_name,
                rel.relname AS referenced_table,
                af.attname AS referenced_column
            FROM
                pg_constraint con
            JOIN
                pg_class t ON t.oid = con.conrelid
            JOIN
                pg_namespace n ON n.oid = t.relnamespace
            LEFT JOIN
                pg_attribute a ON a.attnum = ANY(con.conkey) AND a.attrelid = con.conrelid
            LEFT JOIN
                pg_class rel ON rel.oid = con.confrelid
            LEFT JOIN
                pg_attribute af ON af.attnum = ANY(con.confkey) AND af.attrelid = con.confrelid
             WHERE n.nspname = @SchemaName AND t.relname = @TableName        
            ";

        // Erstellen einer neuen Menge, um die Primärschlüsselspalten zu speichern
        HashSet<string> primaryKeyColumns = new HashSet<string>();

        // HashSet, um die Spalten des Fremdschlüssels zu speichern
        HashSet<string> foreignKeyColumns = new HashSet<string>();

        // Erstellen des SQL-Befehls
        using (var command = new NpgsqlCommand(query, connection))
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
                    if (constraintType == "p")
                    {
                        // Hinzufügen der Primärschlüsselspalte zur Menge
                        primaryKeyColumns.Add(columnName);
                        if (reader["column_name"] != DBNull.Value && foreignKeyColumns.Contains(columnName))
                        {
                            return true;
                        }
                    }

                    if (constraintType == "f")
                    {
                        // Überprüfen, ob ein Fremdschlüssel im Primärschlüssel vorhanden ist
                        foreignKeyColumns.Add(columnName);

                        if (reader["column_name"] != DBNull.Value && primaryKeyColumns.Contains(columnName))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // Rückgabe des Ergebnisses, ob die Tabelle eine schwache Entität ist
        return false;
    }


    /*private void MarkKeys(NpgsqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Primary Keys
        string primaryKeyQuery = @"
            SELECT a.attname AS column_name
            FROM pg_index i
            JOIN pg_attribute a ON a.attnum = ANY(i.indkey)
            WHERE i.indrelid = @SchemaName || '.' || @TableName::regclass::oid
            AND i.indisprimary = TRUE";

        using (var command = new NpgsqlCommand(primaryKeyQuery, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader["column_name"].ToString();
                    var column = columns.Find(c => c.Name == columnName);
                    if (column != null)
                    {
                        column.IsPrimaryKey = true;
                    }
                }
            }
        }

        // Foreign Keys
        string foreignKeyQuery = @"
            SELECT
                kcu.column_name AS column_name,
                ccu.table_name AS referenced_table,
                ccu.column_name AS referenced_column
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
            ON ccu.constraint_name = tc.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY'
            AND tc.table_schema = @SchemaName
            AND tc.table_name = @TableName";

        using (var command = new NpgsqlCommand(foreignKeyQuery, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader["column_name"].ToString();
                    var referencedTable = reader["referenced_table"].ToString();
                    var referencedColumn = reader["referenced_column"].ToString();

                    var column = columns.Find(c => c.Name == columnName);
                    if (column != null)
                    {
                        column.IsForeignKey = true;
                        column.ReferenceTable = new DbTable
                        {
                            Name = referencedTable,
                            Columns = new List<DbColumn>
                            {
                                new DbColumn { Name = referencedColumn }
                            }
                        };
                    }
                }
            }
        }
    }*/


    /// <summary>
    /// Adds constraints to all columns of the table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columns">The list of columns to which constraints will be added.</param>
    private void AddConstraints(NpgsqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Iteration durch jede Spalte in der Liste der Spalten
        foreach (DbColumn col in columns)
        {
            // SQL-Abfrage, um die Constraints der Spalten abzurufen
            string uniqueColumnsQuery = @"
                              SELECT 
                                    con.conname AS constraint_name,
                                    con.contype AS constraint_type,
                                    att.attname AS column_name,
                                    tbl.relname AS table_name,
                                    nsp.nspname AS schema_name,
                                    CASE 
                                        WHEN con.contype = 'c' THEN pg_get_expr(con.conbin, con.conrelid)
                                        ELSE NULL
                                    END AS check_definition,
                                    ref_tbl.relname AS referenced_table, -- Referenced table name
                                    ref_att.attname AS referenced_column -- Referenced column name
                                FROM 
                                    pg_constraint con
                                JOIN 
                                    pg_attribute att 
                                ON 
                                    att.attnum = ANY(con.conkey) AND att.attrelid = con.conrelid
                                JOIN 
                                    pg_class tbl 
                                ON 
                                    tbl.oid = con.conrelid
                                JOIN 
                                    pg_namespace nsp 
                                ON 
                                    tbl.relnamespace = nsp.oid
                                LEFT JOIN 
                                    pg_class ref_tbl 
                                ON 
                                    ref_tbl.oid = con.confrelid -- Join to get the referenced table
                                LEFT JOIN 
                                    pg_attribute ref_att 
                                ON 
                                    ref_att.attnum = ANY(con.confkey) AND ref_att.attrelid = con.confrelid -- Join to get the referenced column
                                WHERE 
                                    tbl.relname = @table
                                AND att.attname = @col;";

            // Erstellen des SQL-Befehls
            using (var command = new NpgsqlCommand(uniqueColumnsQuery, connection))
            {
                // Hinzufügen der Parameter zur SQL-Abfrage
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@col", col.Name);

                // Ausführen des SQL-Befehls und Verarbeiten der Ergebnismenge
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Überprüfen des Typs des Constraints und Setzen der entsprechenden Eigenschaften der Spalte
                        switch (reader["constraint_type"].ToString())
                        {
                            case "p":
                                col.IsPrimaryKey = true;
                                col.IsNull = false;
                                break;
                            case "u":
                                col.IsUnique = true;
                                break;
                            case "f":
                                
                                col.IsForeignKey = true;
                                col.ReferenceColumn = reader["referenced_column"].ToString();
                                col.ReferenceTable = new DbTable() { Name = reader["referenced_table"].ToString() };
                                break;
                            case "c":
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
    public HashSet<List<DbColumn>> GetUniqueCombination(NpgsqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Erstellen einer neuen Menge, um die eindeutigen Spaltenkombinationen zu speichern
        HashSet<List<DbColumn>> uniqueCombinationSet = new HashSet<List<DbColumn>>();

        // SQL-Abfrage, um die eindeutigen Spaltenkombinationen abzurufen
        string sql = @"SELECT 
                    con.conname AS constraint_name,
                    con.contype AS constraint_type,
                    ARRAY(
                        SELECT att.attname 
                        FROM unnest(con.conkey) AS conkey 
                        JOIN pg_attribute att ON att.attnum = conkey AND att.attrelid = con.conrelid
                    ) AS column_names,
                    tbl.relname AS table_name,
                    nsp.nspname AS schema_name
                FROM 
                    pg_constraint con
                JOIN 
                    pg_class tbl ON tbl.oid = con.conrelid
                JOIN 
                    pg_namespace nsp ON tbl.relnamespace = nsp.oid
                WHERE 
                    tbl.relname = @tableName  
                    AND nsp.nspname = @schemaName  
                    AND con.contype = 'u'  -- Nur eindeutige Constraints
                    AND array_length(con.conkey, 1) > 1;  -- Nur zusammengesetzte Constraints (mehr als eine Spalte)
                ";

        // Erstellen des SQL-Befehls
        using (var command = new NpgsqlCommand(sql, connection))
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
        using (var conn = new NpgsqlConnection(connectionString))
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
        using (var conn = new NpgsqlConnection(connectionString))
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



    /// <summary>
    /// Führt ein SQL-Skript aus.
    /// </summary>
    /// <param name="connectionString">Die Verbindungszeichenfolge zur Datenbank.</param>
    /// <param name="sqlScript">Das auszuführende SQL-Skript.</param>
    /// <returns>Gibt true zurück, wenn das Skript erfolgreich ausgeführt wurde, andernfalls false.</returns>
    public void ExecuteScript(string connectionString, string sqlScript)
    {
        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            using (NpgsqlCommand command = new NpgsqlCommand(sqlScript, conn))
            {
                command.ExecuteNonQuery();
            }
            conn.Close();
        }

    }


}

