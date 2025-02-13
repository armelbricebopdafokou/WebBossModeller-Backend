namespace WebBossModellerSqlGenerator.Models
{
    /// <summary>
    /// Stellt ein Datenbankschema dar.
    /// </summary>
    /// <param name="Name">Der Name des Schemas.</param>
    public record DbSchema(string Name) : ISql
    {
        /// <summary>
        /// Liste der Tabellen im Schema.
        /// </summary>
        public List<DbTable> Tables { get; init; } = new List<DbTable>();

        /// <summary>
        /// Erzeugt das SQL-Skript für MSSQL.
        /// </summary>
        /// <returns>Das SQL-Skript für MSSQL.</returns>
        public string ToSqlForMSSSQL()
        {
            return $"\n CREATE SCHEMA [{Name}]; \n";
        }

        /// <summary>
        /// Erzeugt das SQL-Skript für MySQL.
        /// </summary>
        /// <returns>Das SQL-Skript für MySQL.</returns>
        public string ToSqlForMySQL()
        {
            return $"CREATE SCHEMA {Name};";
        }

        /// <summary>
        /// Erzeugt das SQL-Skript für PostgreSQL.
        /// </summary>
        /// <param name="isCaseSensitive">Gibt an, ob der Schemaname case-sensitive ist.</param>
        /// <returns>Das SQL-Skript für PostgreSQL.</returns>
        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            if (isCaseSensitive)
                return $"CREATE SCHEMA IF NOT EXISTS \"{Name}\"; \n";
            else
                return $"CREATE SCHEMA IF NOT EXISTS {Name}; \n";
        }
    }

}
