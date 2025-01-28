using System.Data;
using MySql.Data.MySqlClient;
using WebBossModellerSqlGenerator.Models;

public class MySQLDatabaseService : IDatabaseService
{
    public DbDatabase GetDatabaseSchema(string connectionString, string schemaName)
    {
        DbDatabase database = null;

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            database =  new DbDatabase(connection.Database);

            // Query for schemas
            var schemaTable = connection.GetSchema("Tables");
            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                DbSchema schema = new DbSchema
                (
                    schemaRow["TABLE_SCHEMA"].ToString()!
                );

                schema.Tables.AddRange(LoadTables(connection, schema.Name));
                database.Schemas.Add(schema);
            }
        }

        return database;
    }

    private List<DbTable> LoadTables(MySqlConnection connection, string schemaName)
    {
        List<DbTable> tables = new List<DbTable>();

        var tablesTable = connection.GetSchema("Tables", new string[] { null, schemaName });
        foreach (DataRow tableRow in tablesTable.Rows)
        {
            DbTable table = new DbTable
            {
                Name = tableRow["TABLE_NAME"].ToString()!
            };

            table.Columns.AddRange(LoadColumns(connection, schemaName, table.Name));
            // Identify unique combination
            table.UniqueCombination = GetUniqueColumns(connection, schemaName, table.Name, table.Columns);

            // Mark primary and foreign keys
            MarkKeys(connection, schemaName, table.Name, table.Columns);

            // Detect if it's a weak entity
            table.IsWeak = IsWeakEntity(connection, schemaName, table.Name);

            tables.Add(table);
        }

        return tables;
    }

    private List<DbColumn> LoadColumns(MySqlConnection connection, string schemaName, string tableName)
    {
        List<DbColumn> columns = new List<DbColumn>();

        var columnsTable = connection.GetSchema("Columns", new string[] { null, schemaName, tableName });
        foreach (DataRow columnRow in columnsTable.Rows)
        {
            DbColumn column = new DbColumn
            {
                Name = columnRow["COLUMN_NAME"].ToString()!,
                Type = columnRow["DATA_TYPE"].ToString()!,
                IsNull = columnRow["IS_NULLABLE"].ToString() == "YES",
                DefaultValue = columnRow["COLUMN_DEFAULT"]?.ToString() ?? string.Empty
            };

            columns.Add(column);
        }

        return columns;
    }

    private List<DbColumn> GetUniqueColumns(MySqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        List<DbColumn> uniqueColumns = new List<DbColumn>();

        string uniqueColumnsQuery = @"
            SELECT COLUMN_NAME
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = @SchemaName
            AND TABLE_NAME = @TableName
            AND NON_UNIQUE = 0";

        using (var command = new MySqlCommand(uniqueColumnsQuery, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader["COLUMN_NAME"].ToString();
                    var column = columns.Find(c => c.Name == columnName);
                    if (column != null)
                    {
                        column.IsUnique = true;
                        uniqueColumns.Add(column);
                    }
                }
            }
        }

        return uniqueColumns;
    }

    private void MarkKeys(MySqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
            // Primary Keys
            string primaryKeyQuery = @"
                SELECT COLUMN_NAME
                FROM information_schema.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @TableName
                AND CONSTRAINT_NAME = 'PRIMARY'";

            using (var command = new MySqlCommand(primaryKeyQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader["COLUMN_NAME"].ToString();
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
                SELECT COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
                FROM information_schema.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @TableName
                AND REFERENCED_TABLE_NAME IS NOT NULL";

            using (var command = new MySqlCommand(foreignKeyQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader["COLUMN_NAME"].ToString();
                        var referencedTable = reader["REFERENCED_TABLE_NAME"].ToString();
                        var referencedColumn = reader["REFERENCED_COLUMN_NAME"].ToString();

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
    }

    private bool IsWeakEntity(MySqlConnection connection, string schemaName, string tableName)
    {
        // Query to get columns that are part of the primary key
        string query = @"
            SELECT 
                kcu.COLUMN_NAME, 
                kcu.REFERENCED_TABLE_NAME, 
                kcu.REFERENCED_COLUMN_NAME
            FROM information_schema.KEY_COLUMN_USAGE kcu
            WHERE kcu.TABLE_SCHEMA = @SchemaName
            AND kcu.TABLE_NAME = @TableName
            AND kcu.CONSTRAINT_NAME = 'PRIMARY'";

        bool hasPrimaryKey = false;
        bool hasForeignKeyInPrimaryKey = false;

        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    hasPrimaryKey = true; // Found a primary key column
                    
                    // Check if the primary key column references another table
                    var referencedTable = reader["REFERENCED_TABLE_NAME"];
                    if (referencedTable != DBNull.Value)
                    {
                        hasForeignKeyInPrimaryKey = true;
                    }
                }
            }
        }

        // A table is a weak entity if:
        // - It has a primary key but the primary key includes foreign keys
        // - It depends on another table for identification
        return hasPrimaryKey && hasForeignKeyInPrimaryKey;
    }




}