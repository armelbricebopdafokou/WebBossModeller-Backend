namespace WebBossModellerSqlGenerator.Models
{
    /// <summary>
    /// Stellt eine Datenbank dar.
    /// </summary>
    /// <param name="Name">Der Name der Datenbank.</param>
    public record DbDatabase(string Name) : ISql
    {
        /// <summary>
        /// Standardkonstruktor, der eine leere Datenbank erstellt.
        /// </summary>
        public DbDatabase() : this("") { }

        /// <summary>
        /// Liste der Schemas in der Datenbank.
        /// </summary>
        public List<DbSchema> Schemas { get; init; } = new List<DbSchema>();

        /// <summary>
        /// Erzeugt das SQL-Skript für PostgreSQL.
        /// </summary>
        /// <param name="isCaseSensitive">Gibt an, ob der Datenbankname case-sensitive ist.</param>
        /// <returns>Das SQL-Skript für PostgreSQL.</returns>
        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            if (isCaseSensitive)
                return string.Format("CREATE DATABASE {0}" + Name + "{1}; {2}", "\u0022", "\u0022", Environment.NewLine);
            else
                return $"CREATE DATABASE {Name.ToLower()}; " + Environment.NewLine;
        }

        /// <summary>
        /// Erzeugt das SQL-Skript für MySQL.
        /// </summary>
        /// <returns>Das SQL-Skript für MySQL.</returns>
        public string ToSqlForMySQL()
        {
            return $"CREATE DATABASE  {Name} \n";
        }

        /// <summary>
        /// Erzeugt das SQL-Skript für MSSQL.
        /// </summary>
        /// <returns>Das SQL-Skript für MSSQL.</returns>
        public string ToSqlForMSSSQL()
        {
            return $"CREATE DATABASE [{Name}]; \n";
        }
    }

}
