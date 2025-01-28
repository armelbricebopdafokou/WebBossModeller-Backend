using System.Data;
using Npgsql;
using WebBossModellerSqlGenerator.Models;

public class PostgreSQLDatabaseService : IDatabaseService
{
    public DbDatabase GetDatabaseSchema(string connectionString, string schemaName)
    {
        DbDatabase database=null;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            database = new DbDatabase(connection.Database);

            DbSchema schema = new DbSchema
            (
                schemaName
            );
               
            schema.Tables.AddRange(LoadTables(connection, schema.Name));
            database.Schemas.Add(schema);
    
        }

        return database;
    }

    private List<DbTable> LoadTables(NpgsqlConnection connection, string schemaName)
    {
        List<DbTable> tables = new List<DbTable>();

        var tablesTable = connection.GetSchema("Tables", new string[] { null, schemaName });
        foreach (DataRow tableRow in tablesTable.Rows)
        {
            DbTable table = new DbTable
            {
                Name = tableRow["table_name"].ToString()!
            };

            table.Columns.AddRange(LoadColumns(connection, schemaName, table.Name));
            // Mark primary and foreign keys
            table.UniqueCombination = GetUniqueColumns(connection, schemaName, table.Name, table.Columns);

            
            //MarkKeys(connection, schemaName, table.Name, table.Columns);

            // Detect if it's a weak entity
            table.IsWeak = IsWeakEntity(connection, schemaName, table.Name);

            tables.Add(table);
        }

        return tables;
    }

    private List<DbColumn> LoadColumns(NpgsqlConnection connection, string schemaName, string tableName)
    {
        List<DbColumn> columns = new List<DbColumn>();

        var columnsTable = connection.GetSchema("Columns", new string[] { null, schemaName, tableName });
        foreach (DataRow columnRow in columnsTable.Rows)
        {
            DbColumn column = new DbColumn
            {
                Name = columnRow["column_name"].ToString()!,
                Type = columnRow["data_type"].ToString()!,
                IsNull = columnRow["is_nullable"].ToString() == "YES",
                DefaultValue = columnRow["COLUMN_DEFAULT"]?.ToString() ?? string.Empty
            };

            columns.Add(column);
        }

        return columns;
    }

    private bool IsWeakEntity(NpgsqlConnection connection, string schemaName, string tableName)
    {
        string query = @"
            SELECT
                a.attname AS column_name,
                con.contype AS constraint_type
            FROM pg_attribute a
            JOIN pg_constraint con ON con.conkey @> ARRAY[a.attnum]
            JOIN pg_class t ON t.oid = con.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = @SchemaName AND t.relname = @TableName";

        bool hasPrimaryKey = false;
        bool hasForeignKeyInPrimaryKey = false;

        using (var command = new NpgsqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var constraintType = reader["constraint_type"].ToString();
                    if (constraintType == "p")
                    {
                        hasPrimaryKey = true;
                    }

                    if (constraintType == "f")
                    {
                        hasForeignKeyInPrimaryKey = true;
                    }
                }
            }
        }

        return hasPrimaryKey && hasForeignKeyInPrimaryKey;
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


    private List<DbColumn> GetUniqueColumns(NpgsqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        List<DbColumn> uniqueColumns = new List<DbColumn>();

        foreach(DbColumn col in columns)
        {
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
                                    END AS check_definition
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
                                    WHERE 
                                        tbl.relname = @table
                                        AND att.attname = @col;";

            using (var command = new NpgsqlCommand(uniqueColumnsQuery, connection))
            {
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@col", col.Name);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        switch (reader["constraint_type"].ToString())
                        {
                            case "p":
                                col.IsPrimaryKey = true;
                                col.IsNull = false;
                                col.IsUnique =true;
                            break;
                            case "u":
                                col.IsUnique = true;
                            break;
                            case "f":
                                col.IsForeignKey = true;
                            break;
                            case "c":
                                col.CheckConstraint = reader["check_definition"].ToString();
                            break;
                        }
                        if(col.IsForeignKey == true && col.IsPrimaryKey == true)
                        {
                            uniqueColumns.Add(col);
                        }
                    }
                }
            }
        }

       
        

        return uniqueColumns;
    }


}