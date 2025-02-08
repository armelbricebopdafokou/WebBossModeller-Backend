using WebBossModellerSqlGenerator.Models;
using System.Data.SqlClient;
using System.Data;

public class MSSQLDatabaseService : IDatabaseService
{
    public DbDatabase GetDatabaseSchema(string connectionString, string schemaName)
    {
        DbDatabase database;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            database = new DbDatabase(connection.Database);

            // Load schemas
            DataTable schemaTable = connection.GetSchema("Schemas");
            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                DbSchema schema = new DbSchema
                (
                     schemaRow["schema_name"].ToString()!
                );
                if(schema.Name == schemaName)
                {
                    schema.Tables.AddRange(LoadTables(connection, schema.Name));
                    database.Schemas.Add(schema);
                }
            }
        }

        return database;
    }

    private List<DbTable> LoadTables(SqlConnection connection, string schemaName)
    {
        List<DbTable> tables = new List<DbTable>();

        DataTable tablesTable = connection.GetSchema("Tables", new string[] { null, schemaName });
        foreach (DataRow tableRow in tablesTable.Rows)
        {
            DbTable table = new DbTable
            {
                Name = tableRow["TABLE_NAME"].ToString()!
            };
            // Load columns
            table.Columns.AddRange(LoadColumns(connection, schemaName, table.Name));
            // Identify unique combination
            //table.UniqueCombination = GetUniqueColumns(connection, schemaName, table.Name, table.Columns);

            // Mark primary and foreign keys
            MarkKeys(connection, schemaName, table.Name, table.Columns);

            // Detect if it's a weak entity
            table.IsWeak = IsWeakEntity(connection, schemaName, table.Name);

            tables.Add(table);
        }

        return tables;
    }

    private List<DbColumn> LoadColumns(SqlConnection connection, string schemaName, string tableName)
    {
        List<DbColumn> columns = new List<DbColumn>();

        DataTable columnsTable = connection.GetSchema("Columns", new string[] { null, schemaName, tableName });
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


     private List<DbColumn> GetUniqueColumns(SqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        List<DbColumn> uniqueColumns = new List<DbColumn>();

        string query = @"
            SELECT c.name
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.index_id = ic.index_id AND i.object_id = ic.object_id
            INNER JOIN sys.columns c ON ic.column_id = c.column_id AND ic.object_id = c.object_id
            WHERE i.is_unique = 1 AND OBJECT_SCHEMA_NAME(i.object_id) = @SchemaName AND OBJECT_NAME(i.object_id) = @TableName";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader.GetString(0);
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


    private void MarkKeys(SqlConnection connection, string schemaName, string tableName, List<DbColumn> columns)
    {
        // Primary Keys
        string primaryKeyQuery = @"
            SELECT c.name
            FROM sys.key_constraints kc
            INNER JOIN sys.index_columns ic ON kc.unique_index_id = ic.index_id AND kc.parent_object_id = ic.object_id
            INNER JOIN sys.columns c ON ic.column_id = c.column_id AND ic.object_id = c.object_id
            WHERE kc.type = 'PK' AND OBJECT_SCHEMA_NAME(kc.parent_object_id) = @SchemaName AND OBJECT_NAME(kc.parent_object_id) = @TableName";

        using (SqlCommand command = new SqlCommand(primaryKeyQuery, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader.GetString(0);
                    var column = columns.Find(c => c.Name == columnName);
                    if (column != null)
                        column.IsPrimaryKey = true;
                }
            }
        }

        // Foreign Keys
        string foreignKeyQuery = @"
            SELECT
                fk.name AS FK_Name,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = @SchemaName AND OBJECT_NAME(fk.parent_object_id) = @TableName";

        using (SqlCommand command = new SqlCommand(foreignKeyQuery, connection))
        {
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader["ColumnName"].ToString();
                    var referenceTableName = reader["ReferencedTable"].ToString();
                    var referencedColumnName = reader["ReferencedColumn"].ToString();

                    var column = columns.Find(c => c.Name == columnName);
                    if (column != null)
                    {
                        column.IsForeignKey = true;
                        column.ReferenceTable = new DbTable
                        {
                            Name = referenceTableName,
                            Columns = new List<DbColumn>
                            {
                                new DbColumn { Name = referencedColumnName }
                            }
                        };
                    }
                }
            }
        }
    }

private bool IsWeakEntity(SqlConnection connection, string schemaName, string tableName)
{
    // Check if primary key includes a foreign key
    string query = @"
        SELECT 
            c.name AS ColumnName,
            kc.type AS ConstraintType
        FROM sys.key_constraints kc
        INNER JOIN sys.index_columns ic ON kc.unique_index_id = ic.index_id AND kc.parent_object_id = ic.object_id
        INNER JOIN sys.columns c ON ic.column_id = c.column_id AND ic.object_id = c.object_id
        LEFT JOIN sys.foreign_key_columns fkc ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE OBJECT_SCHEMA_NAME(kc.parent_object_id) = @SchemaName
        AND OBJECT_NAME(kc.parent_object_id) = @TableName";

        HashSet<string> primaryKeyColumns = new HashSet<string>();
        bool hasForeignKeyInPrimaryKey = false;

    using (SqlCommand command = new SqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableName", tableName);

        using (SqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var columnName = reader["ColumnName"].ToString();
                var constraintType = reader["ConstraintType"].ToString();
                if (constraintType == "PK")
                {
                   primaryKeyColumns.Add(columnName);
                }

                if (constraintType == "PK" && reader["ColumnName"] != DBNull.Value && primaryKeyColumns.Contains(columnName))
                {
                    hasForeignKeyInPrimaryKey = true;
                }
            }
        }
    }

    // A table is a weak entity if:
    // - It has a primary key, but the primary key includes foreign keys.
    // - It does not have a strong primary key of its own.
    return  hasForeignKeyInPrimaryKey;
}


}