namespace WebBossModellerSqlGenerator.Models
{
    public record DbDatabase(string Name) : ISql
    {

        public DbDatabase():this(""){}
        public List<DbSchema> Schemas { get; init; } = new List<DbSchema>();   

      
        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            if(isCaseSensitive == true)
                return string.Format("CREATE DATABASE {0}"+Name+"{1}; {2}", "\u0022", "\u0022", Environment.NewLine);
            else
                return $"CREATE DATABASE {Name.ToLower()}; "+Environment.NewLine;
        }

        public string ToSqlForMySQL()
        {
            return $"CREATE DATABASE IF NOT EXISTS {Name} \n";
        }

        public string ToSqlForMSSSQL()
        {
            return $"CREATE DATABASE [{Name}] \n GO \n";
        }

       
    }
}
