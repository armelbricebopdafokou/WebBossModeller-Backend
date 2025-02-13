namespace WebBossModellerSqlGenerator.Models
{
    // DbColumn repräsentiert eine Datenbankspalte und implementiert das ISql-Interface.
    public record DbColumn : ISql
    {
        // Name der Spalte
        public string Name { get; init; }

        // Datentyp der Spalte (z.B. INT, VARCHAR)
        public string Type { get; init; }

        // Gibt an, ob die Spalte NULL-Werte erlaubt
        public bool IsNull { get; set; }

        // Gibt an, ob die Spalte eindeutige Werte haben muss
        public bool IsUnique { get; set; } = false;

        // Gibt an, ob die Spalte Teil des Primärschlüssels ist
        public bool IsPrimaryKey { get; set; } = false;

        // Gibt an, ob die Spalte ein Fremdschlüssel ist
        public bool IsForeignKey { get; set; }

        // Check-Constraint für die Spalte (z.B. "Alter > 0")
        public string? CheckConstraint { get; set; }

        // Standardwert für die Spalte
        public string? DefaultValue { get; init; } = string.Empty;

        // Referenzierte Tabelle (für Fremdschlüssel)
        public DbTable? ReferenceTable { get; set; }

        // Referenzierte Spalte (für Fremdschlüssel)
        public string? ReferenceColumn { get; set; }

        // Generiert SQL für Microsoft SQL Server
        public string ToSqlForMSSSQL()
        {
            // Validierung der Spalte
            Validate();

            // SQL-String für die Spalte
            var sqlCreate = $"[{Name}]  {Type} ";

            // Fügt UNIQUE hinzu, wenn die Spalte eindeutig ist
            if (IsUnique) sqlCreate += " UNIQUE ";

            // Fügt NULL/NOT NULL und DEFAULT-Wert hinzu
            sqlCreate += NullAndDefault();

            // Fügt CHECK-Constraint hinzu, falls vorhanden
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += $" CHECK ([{Name}] {CheckConstraint})";

            // Fügt Fremdschlüssel-Constraint hinzu, falls vorhanden
            //if (IsForeignKey && ReferenceTable != null)
            //{
            //    sqlCreate += $", FOREIGN KEY ([{Name}]) REFERENCES [{ReferenceTable.Name}]([{ReferenceColumn}])";
            //}

            return sqlCreate;
        }

        // Generiert SQL für MySQL
        public string ToSqlForMySQL()
        {
            // Validierung der Spalte
            Validate();

            // SQL-String für die Spalte
            var sqlCreate = $"{Name}  {Type} ";

            // Fügt UNIQUE hinzu, wenn die Spalte eindeutig ist
            if (IsUnique) sqlCreate += " UNIQUE ";

            // Fügt NULL/NOT NULL und DEFAULT-Wert hinzu
            sqlCreate += NullAndDefault();

            // Fügt CHECK-Constraint hinzu, falls vorhanden
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += $" CHECK {CheckConstraint}";

            // Fügt Fremdschlüssel-Constraint hinzu, falls vorhanden
            if (IsForeignKey && ReferenceTable != null)
            {
                sqlCreate += $", FOREIGN KEY ({Name}) REFERENCES {ReferenceTable.Name}({ReferenceColumn})";
            }

            return sqlCreate;
        }

        // Generiert SQL für PostgreSQL
        public string ToSqlForPostgresSQL(bool isCaseSensitive)
        {
            // Validierung der Spalte
            Validate();

            // Behandelt Groß-/Kleinschreibung in PostgreSQL
            string columnName = isCaseSensitive ? $"\"{Name}\"" : Name;

            // SQL-String für die Spalte
            var sqlCreate = $"{columnName}  {Type} ";

            // Fügt UNIQUE hinzu, wenn die Spalte eindeutig ist
            if (IsUnique) sqlCreate += " UNIQUE ";

            // Fügt NULL/NOT NULL und DEFAULT-Wert hinzu
            sqlCreate += NullAndDefault();

            // Fügt CHECK-Constraint hinzu, falls vorhanden
            if (!string.IsNullOrEmpty(CheckConstraint)) sqlCreate += isCaseSensitive? $" CHECK (\"{this.Name}\" {CheckConstraint})": $" CHECK ({this.Name} {CheckConstraint})";

            // Fügt Fremdschlüssel-Constraint hinzu, falls vorhanden
            //if (IsForeignKey && ReferenceTable != null)
            //{
            //    string refTableName = isCaseSensitive ? $"\"{ReferenceTable.Name}\"" : ReferenceTable.Name;
            //    string refColumnName = isCaseSensitive ? $"\"{ReferenceColumn}\"" : ReferenceColumn;
            //    sqlCreate += $", FOREIGN KEY ({columnName}) REFERENCES {refTableName}({refColumnName})";
            //}

            return sqlCreate;
        }

        // Hilfsmethode für NULL/NOT NULL und DEFAULT-Wert
        private string NullAndDefault()
        {
            string sqlCreate = string.Empty;

            // Fügt NOT NULL hinzu, wenn die Spalte keine NULL-Werte erlaubt
            if (!IsNull) sqlCreate += " NOT NULL";

            // Fügt DEFAULT-Wert hinzu, falls vorhanden
            if (!string.IsNullOrEmpty(DefaultValue)) sqlCreate += " DEFAULT " + DefaultValue;

            return sqlCreate;
        }

        // Validierung der Spalte
        private void Validate()
        {
            // Überprüft, ob die Spalte ein Fremdschlüssel ist und eine referenzierte Tabelle und Spalte hat
            if (IsForeignKey && (ReferenceTable == null || string.IsNullOrEmpty(ReferenceColumn)))
                throw new InvalidOperationException("Ein Fremdschlüssel muss eine Tabelle und eine Spalte referenzieren.");
        }
    }
}