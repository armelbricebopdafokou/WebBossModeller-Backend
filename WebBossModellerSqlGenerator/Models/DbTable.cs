using WebBossModellerSqlGenerator.DTO;

namespace WebBossModellerSqlGenerator.Models
{
    public record DbTable: ISql
    {
        public string Name { get; init; }
        public bool IsWeak { get; set; }
        public List<DbColumn> Columns { get; set; } = new List<DbColumn>();
        public List<DbColumn> UniqueCombination { get;  set; } = new List<DbColumn>();
        public DbSchema? Schema { get; init; }

      

        public string ToSqlForMSSSQL()
        {
            var sql = $"CREATE TABLE [{Schema.Name}].[{Name}] ({Environment.NewLine}";
            foreach (var col in Columns)
            {
                sql += col.ToSqlForMSSSQL() +", \n" ;
            }
            sql += ");";
            return sql ;
        }
        public void AddUniqueCombination(params DbColumn[] columns)
        {
            this.UniqueCombination = columns.Distinct().ToList();
        }
        public string ToSqlForMySQL()
        {
            var sql = $"CREATE {Name} ({Environment.NewLine}";
            foreach (var col in Columns)
            {
                sql += col.ToSqlForMSSSQL() + "\n";
            }
            sql += ");";
            return sql;
        }

        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            string sql=string.Empty ;
            if(isCaseSensitive)
                sql = $"CREATE TABLE  \"{Name}\" ({Environment.NewLine}";
            else
                sql = $"CREATE TABLE {Name} ({Environment.NewLine}";


            foreach (var col in Columns)
            {
                sql += col.ToSqlForPostgresSQL(isCaseSensitive) + "\n";
            }
            sql += ");";
            return sql;
        }

        public string AddContrainstsPostgres(bool isSensitive=false)
        {
            if (Columns == null )
                throw new Exception("No columns or to key");

            if(Columns != null && Columns.Where(c=> c.IsPrimaryKey && c.IsForeignKey).Count() > 0)
                throw new Exception("A key cannot be a primary and a foreign key");

            string sql = string.Empty ;
            var cols = GetPrimaryKey();
            if (cols != null && cols.Length > 0)
            {
                if(isSensitive == true)
                {
    	            sql = $"\n ALTER TABLE \"{Name}\" ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                    for (int i = 0; i < cols.Length; i++)
                    {
                        sql += $" \"{cols[i].Name}\"";
                   
                        if (i < cols.Length - 1)
                            sql += ",";
                    }
                }
                else
                {
                    sql = $"\n ALTER TABLE {Name} ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                    for (int i = 0; i < cols.Length; i++)
                    {
                   
                        sql += cols[i].Name;
                    
                        if (i < cols.Length - 1)
                        sql += ",";
                    }
                }                
                sql += "); \n";
            }
            if(UniqueCombination.Count() > 0)
            {
                if(isSensitive == true)
                {
                    sql += $"\n ALTER TABLE \"{this.Name}\"  ADD UNIQUE {UniqueCombination[0].Name.Substring(0, 2)}_uniq (";
                    for (short i = 0; i < UniqueCombination.Count; i++)
                    {
                        if (i == UniqueCombination.Count - 1)
                        {
                            sql += $"\"{UniqueCombination[i].Name}\"";
                        }
                        else
                        {
                            sql += $"\"{UniqueCombination[i].Name}\",";
                        }
                    }
                }
                else
                {
                    sql += $"\n ALTER TABLE {this.Name}  ADD UNIQUE {UniqueCombination[0].Name.Substring(0, 2)}_uniq (";
                    for (short i = 0; i < UniqueCombination.Count; i++)
                    {
                        if (i == UniqueCombination.Count - 1)
                        {
                            sql += $"{UniqueCombination[i].Name}";
                        }
                        else
                        {
                            sql += $"{UniqueCombination[i].Name},";
                        }
                    }
                }
               
                sql += ");\n";
            }
           

            foreach (var c in  Columns.Where(co => co.IsForeignKey))
            {
                if(isSensitive == true)
                {
                    sql += $" ALTER TABLE \"{this.Name}\" ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk FOREIGN KEY (\"{c.Name}\") REFERENCES \"{c.ReferenceTable.Name}\" (\"{c.ReferenceTable.GetPrimaryKey().First().Name}\"); \n ";
                }
                else
                {
                    sql += $" ALTER TABLE {this.Name} ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk FOREIGN KEY ({c.Name}) REFERENCES {c.ReferenceTable.Name} ({c.ReferenceTable.GetPrimaryKey().First().Name}); \n";
                }
            }
            return sql;
        }

        public DbColumn[] GetPrimaryKey()
        {
            List<DbColumn> list = new List<DbColumn>();
            foreach (var primary in Columns)
            {
                if (primary.IsPrimaryKey == true && primary.IsNull == true)
                {
                    throw new Exception("A primary key must not be null");
                }
                else if (primary.IsPrimaryKey == true)
                {
                    list.Add(primary);
                }
            }
            return list.ToArray();
        }

        public string AddContrainstsMSSQL()
        {
            if (Columns == null )
                throw new Exception($"No columns ");

            if (Columns != null && Columns.Where(c => c.IsPrimaryKey == true && c.IsForeignKey == true).Count() > 0)
                throw new Exception("A key cannot be a primary and a foreign key");

            string sql = string.Empty;
            var cols = GetPrimaryKey();
            if(cols!= null && cols.Length > 0)
            {
                sql = $"\n ALTER TABLE [{Name}] ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                for(int i=0; i<cols.Length;i++)
                {
                    sql += "[" + cols[i].Name + "]";
                    if(i<cols.Length-1)
                        sql += ",";
                }
                sql += "); \n";
            }

            if (UniqueCombination.Count > 0)
            {
                sql += $"\n ALTER TABLE [{this.Name}]  ADD UNIQUE {UniqueCombination[0].Name.Substring(0, 2)}_uniq (";
                for (short i = 0; i < UniqueCombination.Count; i++)
                {
                    if (i == UniqueCombination.Count - 1)
                    {
                        sql += $"{UniqueCombination[i].Name}";
                    }
                    else
                    {
                        sql += $"{UniqueCombination[i].Name},";
                    }
                }
                sql += ");\n";
            }

            foreach (var c in Columns.Where(co => co.IsForeignKey))
            {
                 sql += $"\n ALTER TABLE {this.Name} ADD CONSTRAINT {this.Name.Substring(0,3)}_{c.ReferenceTable.Name.Substring(0,3)}_fk  FOREIGN KEY ([{c.Name}]) REFERENCES [{c.ReferenceTable.Name}] ([{c.ReferenceTable.GetPrimaryKey().First().Name}]); \n";
            }

            return sql ;
        }

        public string AddContrainstsMySQL()
        {
            if (Columns == null )
                throw new Exception("No columns ");

            if (Columns != null && Columns.Where(c => c.IsPrimaryKey && c.IsForeignKey).Count() > 0)
                throw new Exception("A key cannot be a primary and a foreign key");

            string sql = string.Empty;
            var cols = GetPrimaryKey();

            if (cols != null && cols.Length > 0)
            {
                sql = $"\n ALTER TABLE {Name} ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                for (int i = 0; i < cols.Length; i++)
                {
                    sql +=  cols[i].Name;
                    if (i < cols.Length - 1)
                        sql += ",";
                }
                sql += ");\n";
            }

            if (UniqueCombination.Count > 0)
            {
                sql += $"\n ALTER TABLE {this.Name}  ADD UNIQUE {UniqueCombination[0].Name.Substring(0, 2)}_uniq (";
                for (short i = 0; i < UniqueCombination.Count; i++)
                {
                    if (i == UniqueCombination.Count - 1)
                    {
                        sql += $"{UniqueCombination[i].Name}";
                    }
                    else
                    {
                        sql += $"{UniqueCombination[i].Name},";
                    }
                }
                sql += ");\n";
            }
           

            foreach (var c in Columns.Where(co => co.IsForeignKey))
            {
                sql += $"\n ALTER TABLE {this.Name}  ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk  FOREIGN KEY ({c.Name}) REFERENCES {c.ReferenceTable.Name} ({c.ReferenceTable.GetPrimaryKey().First().Name}); \n";
            }

            return sql;
        }

       
    }
}
