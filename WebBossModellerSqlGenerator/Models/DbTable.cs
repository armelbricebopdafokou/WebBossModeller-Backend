﻿using WebBossModellerSqlGenerator.DTO;

namespace WebBossModellerSqlGenerator.Models
{
    public record DbTable: ISql
    {
        public string Name { get; init; }
        public bool IsWeak { get; init; }
        public List<DbColumn> Columns { get; set; }

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
            if (Columns != null )
                throw new Exception("No columns or too much primary key");

            if(Columns != null && Columns.Where(c=> c.IsPrimaryKey && c.IsForeignKey).Count() > 0)
                throw new Exception("A key cannot be a primary and a foreign key");

            string sql = string.Empty ;
            var cols = GetPrimaryKey();
            if (cols != null && cols.Length > 0)
            {
                sql = $"\n ALTER TABLE [{Name}] ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                for (int i = 0; i < cols.Length; i++)
                {
                    if (isSensitive == true)
                    {
                        sql += $" \"{cols[i].Name}\"";
                    }
                    else
                    {
                        sql += cols[i].Name;
                    }
                    if (i < cols.Length - 1)
                        sql += ",";
                }
                sql = ");";
            }
            
                
            foreach(var c in  Columns.Where(co => co.IsForeignKey))
            {
                if(isSensitive == true)
                {
                    sql += $" ALTER TABLE ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk FOREIGN KEY (\"{c.Name}\") REFERENCES \"{c.ReferenceTable.Name}\" (\"{c.ReferenceTable.GetPrimaryKey()}\"); \n ";
                }
                else
                {
                    sql += $" ALTER TABLE ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk FOREIGN KEY ({c.Name}) REFERENCES {c.ReferenceTable.Name} ({c.ReferenceTable.GetPrimaryKey()}); \n";
                }
            }
            return sql;
        }

        public DbColumn[] GetPrimaryKey()
        {
            return Columns.Where(col => col.IsPrimaryKey == true).ToArray();
        }

        public string AddContrainstsMSSQL()
        {
            if (Columns != null )
                throw new Exception($"No columns or too much primary key ");

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
                sql = ");";
            }
            
           
            foreach (var c in Columns.Where(co => co.IsForeignKey))
            {
                 sql += $"\n ALTER TABLE ADD CONSTRAINT {this.Name.Substring(0,3)}_{c.ReferenceTable.Name.Substring(0,3)}_fk  FOREIGN KEY ([{c.Name}]) REFERENCES [{c.ReferenceTable.Name}] ([{c.ReferenceTable.GetPrimaryKey()}]); \n";
            }

            return sql ;
        }

        public string AddContrainstsMySQL()
        {
            if (Columns != null )
                throw new Exception("No columns or too much primary key");

            if (Columns != null && Columns.Where(c => c.IsPrimaryKey && c.IsForeignKey).Count() > 0)
                throw new Exception("A key cannot be a primary and a foreign key");

            string sql = string.Empty;
            var cols = GetPrimaryKey();

            if (cols != null && cols.Length > 0)
            {
                sql = $"\n ALTER TABLE [{Name}] ADD CONSTRAINT " + this.Name.Substring(0, 3) + "_pk  PRIMARY KEY (";
                for (int i = 0; i < cols.Length; i++)
                {
                    sql +=  cols[i].Name;
                    if (i < cols.Length - 1)
                        sql += ",";
                }
                sql = ");";
            }

            foreach (var c in Columns.Where(co => co.IsForeignKey))
            {
                sql += $"ALTER TABLE ADD CONSTRAINT {this.Name.Substring(0, 3)}_{c.ReferenceTable.Name.Substring(0, 3)}_fk  FOREIGN KEY ({c.Name}) REFERENCES {c.ReferenceTable.Name} ({c.ReferenceTable.GetPrimaryKey()}); \n";
            }

            return sql;
        }

       
    }
}
