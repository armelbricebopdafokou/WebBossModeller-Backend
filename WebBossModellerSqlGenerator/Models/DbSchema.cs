namespace WebBossModellerSqlGenerator.Models
{
    public record DbSchema(string Name) :ISql
    {
        public List<DbTable> Tables { get; init; }
        public string ToSqlForMSSSQL()
        {
            return $"\n CREATE SCHEMA [{Name}]; \n GO \n";
        }

        public string ToSqlForMySQL()
        {
            return $"CREATE SCHEMA {Name}";
        }

        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            if (isCaseSensitive) 
                return $"CREATE SCHEMA \"{Name}\" \n";
            else
                return $"CREATE SCHEMA {Name} \n";
        }
    }
}
