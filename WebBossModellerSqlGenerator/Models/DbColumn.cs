namespace WebBossModellerSqlGenerator.Models
{
    public record DbColumn: ISql
    {
        public string Name { get; init; }
        public string Type { get; init; }
        public bool IsNull { get; set; }
        public bool IsUnique { get; set; } = false;
        public bool IsPrimaryKey { get; set; }=false;
        public bool IsForeignKey { get; set; }
        public string? CheckConstraint {get; set; }
        public string? DefaultValue { get; init; } = string.Empty;
        public DbTable ReferenceTable { get; set; }

        public string ToSqlForMSSSQL()
        {
            if (IsPrimaryKey == true && IsForeignKey == true)
                throw new Exception("A key cannot be a primary and a foreign key");

            var sqlCreate = $"[{Name}]  {Type} ";
            if (IsUnique == true) sqlCreate += " UNIQUE ";
            
            sqlCreate += NullAndDefault();
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += $" CHECK {CheckConstraint}";
            return sqlCreate ;
        }

        public string ToSqlForMySQL()
        {
            if (IsPrimaryKey == true && IsForeignKey == true)
                throw new Exception("A key cannot be a primary and a foreign key");

            var sqlCreate = $"{Name}  {Type} ";            
            if (IsUnique == true) sqlCreate += " UNIQUE ";
            
            sqlCreate += NullAndDefault();
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += $" CHECK {CheckConstraint}";
            return sqlCreate;
        }

        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            if (IsPrimaryKey == true && IsForeignKey == true)
                throw new Exception("A key cannot be a primary and a foreign key");
            string sqlCreate = string.Empty ;
            if (isCaseSensitive)
            {
                sqlCreate = $"\"{Name}\"  {Type} ";
            }
               
            else
                sqlCreate = $"{Name}  {Type} ";
            if (IsUnique == true) sqlCreate += " UNIQUE ";
            //if (IsPrimaryKey == true)
              //  sqlCreate += " PRIMARY KEY ";
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += $" CHECK {CheckConstraint}";

            sqlCreate += NullAndDefault();
            
            return sqlCreate;
        }

        private string NullAndDefault()
        {
            string sqlCreate = string.Empty;
            if (!IsNull) sqlCreate += " NOT NULL";
            if (!string.IsNullOrEmpty(DefaultValue)) sqlCreate += " DEFAULT " + DefaultValue;
            return sqlCreate;
        }
        
     

    }
}
