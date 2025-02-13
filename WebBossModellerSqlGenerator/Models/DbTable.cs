using WebBossModellerSqlGenerator.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebBossModellerSqlGenerator.Models
{
    // DbTable repräsentiert eine Datenbanktabelle und implementiert das ISql-Interface.
    public record DbTable : ISql
    {
        // Name der Tabelle
        public string Name { get; set; }

        // Gibt an, ob die Tabelle eine schwache Entität ist
        public bool IsWeak { get; set; }

        // Liste der Spalten in der Tabelle
        public List<DbColumn> Columns { get; set; } = new List<DbColumn>();

        // Liste der Spalten, die eine eindeutige Kombination bilden
        public HashSet<List<DbColumn>> UniqueCombination { get; set; } = new HashSet<List<DbColumn>>();

        // Schema, zu dem die Tabelle gehört
        public DbSchema? Schema { get; init; }


        // Generiert SQL-Code für Microsoft SQL Server
        public string ToSqlForMSSSQL()
        {
            var sql = $"CREATE TABLE [{Schema?.Name}].[{Name}] ({Environment.NewLine}";
            for (int i = 0; i < Columns.Count; i++)
            {

                if (i == Columns.Count - 1)
                    sql += Columns[i].ToSqlForMSSSQL() + " \n";
                else
                    sql += Columns[i].ToSqlForMSSSQL() + ", \n";
            }
            sql = sql[..^1];
            sql += ");";
            return sql;
        }

        

        // Generiert SQL-Code für MySQL
        public string ToSqlForMySQL()
        {
            var sql = $"CREATE TABLE {Name} ({Environment.NewLine}";
            for (int i = 0; i < Columns.Count; i++)
            {
                if (i == Columns.Count - 1)
                    sql += Columns[i].ToSqlForMySQL() + "\n";
                else
                    sql += Columns[i].ToSqlForMySQL() + ",\n";
            }
           
            sql += ");";
            return sql;
        }

        // Generiert SQL-Code für PostgreSQL
        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            string sql = isCaseSensitive ? $"DROP TABLE IF EXISTS \"{Name}\";  {Environment.NewLine} CREATE TABLE  \"{Name}\" ({Environment.NewLine}" : $"CREATE TABLE {Name} ({Environment.NewLine}";
            for (int i =0; i< Columns.Count; i++)
            {
                if (i == Columns.Count - 1)
                    sql += Columns[i].ToSqlForPostgresSQL(isCaseSensitive) + "\n";
                else
                    sql += Columns[i].ToSqlForPostgresSQL(isCaseSensitive) + ",\n";
            }
            sql += ");";
            return sql;
        }

        // Fügt Constraints (Primärschlüssel, eindeutige Kombinationen, Fremdschlüssel) für PostgreSQL hinzu
        public string AddConstraintsPostgres(bool isCaseSensitive = false)
        {
            ValidateTable();

            //PrimärSchlüssel Constrainst
            string sql = GeneratePrimaryKeyConstraintPostgres(Name, GetPrimaryKey(), isCaseSensitive);
            foreach(var uniquCombis  in UniqueCombination)
            {
                sql += GenerateUniqueCombinationConstraintPostgres(Name, uniquCombis, isCaseSensitive);
            }
            
            //Foreign Schlüssel Constraint
            foreach (var col in Columns.Where(c => c.IsForeignKey))
            {
                sql += GenerateForeignKeyConstraintPostgres(Name, col, isCaseSensitive);
            }

            return sql;
        }

        // Fügt Constraints (Primärschlüssel, eindeutige Kombinationen, Fremdschlüssel) für Microsoft SQL Server hinzu
        public string AddConstraintsMSSQL()
        {
            ValidateTable();

            string sql = GeneratePrimaryKeyConstraintMSSQL(GetPrimaryKey());
            foreach(var uniquCombis in UniqueCombination)
            {
                sql += GenerateUniqueCombinationConstraintMSSQL(uniquCombis);
            }
           
            foreach (var col in Columns.Where(c => c.IsForeignKey))
            {
                sql += GenerateForeignKeyConstraintMSSQL(col);
            }

            return sql;
        }

        // Fügt Constraints (Primärschlüssel, eindeutige Kombinationen, Fremdschlüssel) für MySQL hinzu
        public string AddConstraintsMySQL()
        {
            ValidateTable();

            string sql = GeneratePrimaryKeyConstraintPostgres(Name, GetPrimaryKey());
            foreach (var uniquCombis in UniqueCombination)
            {
                sql += GenerateUniqueCombinationConstraintPostgres(Name, uniquCombis);
            }

            foreach (var col in Columns.Where(c => c.IsForeignKey))
            {
                sql += GenerateForeignKeyConstraintMSSQL(col);
            }

            return sql;
        }

        // Gibt die Primärschlüssel-Spalten der Tabelle zurück
        public DbColumn[] GetPrimaryKey()
        {
            return Columns.Where(c => c.IsPrimaryKey).ToArray();
        }

        // Validiert die Tabelle (z. B. ob Spalten definiert sind und keine Spalte gleichzeitig Primär- und Fremdschlüssel ist)
        private void ValidateTable()
        {
            if (Columns == null || !Columns.Any())
                throw new ArgumentException("Keine Spalten für die Tabelle definiert.");

            
        }

        // Generiert SQL-Code für einen Primärschlüssel-Constraint
        private string GeneratePrimaryKeyConstraintPostgres(string tableName, DbColumn[] primaryKeyColumns, bool isCaseSensitive = false)
        {
            if (primaryKeyColumns == null || primaryKeyColumns.Length == 0)
                return string.Empty;

            string sql = $"\n ALTER TABLE {(isCaseSensitive ? $"\"{tableName}\"" : $"{tableName}")} " +
                         $"ADD CONSTRAINT {tableName.Substring(0, 3)}_{Guid.NewGuid().ToString().Substring(0,5)}_pk PRIMARY KEY (";

            for (int i = 0; i < primaryKeyColumns.Length; i++)
            {
                sql += (isCaseSensitive ? $"\"{primaryKeyColumns[i].Name}\"" : $"{primaryKeyColumns[i].Name}");
                if (i < primaryKeyColumns.Length - 1)
                    sql += ",";
            }

            sql += ");\n";
            return sql;
        }

        // Generiert SQL-Code für einen Primärschlüssel-Constraint
        private string GeneratePrimaryKeyConstraintMSSQL(DbColumn[] primaryKeyColumns)
        {
            if (primaryKeyColumns == null || primaryKeyColumns.Length == 0)
                return string.Empty;

            string sql = $"\n ALTER TABLE [{Schema?.Name}].[{Name}] " +
                         $"ADD CONSTRAINT {Name.Substring(0, 3)}_{Guid.NewGuid().ToString().Substring(0, 5)}_pk PRIMARY KEY (";

            for (int i = 0; i < primaryKeyColumns.Length; i++)
            {
                sql +=  $"[{primaryKeyColumns[i].Name}]";
                if (i < primaryKeyColumns.Length - 1)
                    sql += ",";
            }

            sql += ");\n";
            return sql;
        }

        // Generiert SQL-Code für einen Unique-Constraint (eindeutige Kombination von Spalten) für Postgres
        private string GenerateUniqueCombinationConstraintPostgres(string tableName, List<DbColumn> uniqueColumns, bool isCaseSensitive = false)
        {
            if (uniqueColumns == null || !uniqueColumns.Any())
                return string.Empty;

            string sql = $"\n ALTER TABLE {(isCaseSensitive ? $"\"{tableName}\"" : $"{tableName}")} " +
                         $"ADD UNIQUE (";

            for (int i = 0; i < uniqueColumns.Count; i++)
            {
                sql += (isCaseSensitive ? $"\"{uniqueColumns[i].Name}\"" : $"{uniqueColumns[i].Name}");
                if (i < uniqueColumns.Count - 1)
                    sql += ",";
            }

            sql += ");\n";
            return sql;
        }

        // Generiert SQL-Code für einen Unique-Constraint (eindeutige Kombination von Spalten) für MSSQL
        private string GenerateUniqueCombinationConstraintMSSQL(List<DbColumn> uniqueColumns)
        {
            if (uniqueColumns == null || !uniqueColumns.Any())
                return string.Empty;

            string sql = $"\n ALTER TABLE [{Schema?.Name}].[{Name}] " +
                         $"ADD UNIQUE (";

            for (int i = 0; i < uniqueColumns.Count; i++)
            {
                sql += ($"[{uniqueColumns[i].Name}]");
                if (i < uniqueColumns.Count - 1)
                    sql += ",";
            }

            sql += ");\n";
            return sql;
        }



        // Generiert SQL-Code für einen Fremdschlüssel-Constraint für PostgresSQL
        private string GenerateForeignKeyConstraintPostgres(string tableName, DbColumn foreignKeyColumn, bool isCaseSensitive = false)
        {
            if (foreignKeyColumn == null || !foreignKeyColumn.IsForeignKey || foreignKeyColumn.ReferenceTable == null)
                return string.Empty;

            string sql = $"\n ALTER TABLE {(isCaseSensitive ? $"\"{tableName}\"" : $"{tableName}")} " +
                         $"ADD CONSTRAINT {tableName.Substring(0, 3)}_{foreignKeyColumn.ReferenceTable.Name.Substring(0, 3)}_fk " +
                         $"FOREIGN KEY ({(isCaseSensitive ? $"\"{foreignKeyColumn.Name}\"" : $"{foreignKeyColumn.Name}")}) " +
                         $"REFERENCES {(isCaseSensitive ? $"\"{foreignKeyColumn.ReferenceTable.Name}\"" : $"{foreignKeyColumn.ReferenceTable.Name}")} " +
                         $"({(isCaseSensitive ? $"\"{foreignKeyColumn.ReferenceTable.GetPrimaryKey().First().Name}\"" : $"{foreignKeyColumn.ReferenceColumn}")});\n";

            return sql;
        }

        // Generiert SQL-Code für einen Fremdschlüssel-Constraint für MSSQL
        private string GenerateForeignKeyConstraintMSSQL(DbColumn foreignKeyColumn)
        {
            if (foreignKeyColumn == null || !foreignKeyColumn.IsForeignKey || foreignKeyColumn.ReferenceTable == null)
                return string.Empty;

            string sql = $"\n ALTER TABLE [{Schema?.Name}].[{Name}] " +
                         $"ADD CONSTRAINT {Name.Substring(0, 3)}_{foreignKeyColumn.ReferenceTable.Name.Substring(0, 3)}_fk " +
                         $"FOREIGN KEY ([{foreignKeyColumn.Name}]) " +
                         $"REFERENCES [{foreignKeyColumn.ReferenceTable.Schema?.Name}].[{foreignKeyColumn.ReferenceTable.Name}]" +
                         $"([{foreignKeyColumn.ReferenceColumn}]);\n";

            return sql;
        }
    }
}