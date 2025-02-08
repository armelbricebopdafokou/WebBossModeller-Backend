

using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    /// <summary>
    /// The `ColumnDTO` class is a Data Transfer Object (DTO) used to represent metadata about a database column.
    /// It is typically used to transfer column-related information between different layers of an application,
    /// such as between the data access layer and the business logic or presentation layers.
    /// This class encapsulates properties like the column name, data type, constraints (e.g., NOT NULL, UNIQUE),
    /// and relationships (e.g., primary key, foreign key) in a structured format.
    /// </summary>
    public class ColumnDTO
    {
        // Der Name der Spalte in der Datenbank.
        public string Name { get; set; }

        // Der Datentyp der Spalte (z.B. "int", "varchar", etc.). Kann null sein.
        public string? Type { get; set; }

        // Der Standardwert der Spalte, falls einer definiert ist. Kann null sein.
        public string? DefaultValue { get; set; }

        // Ein Check-Constraint, das auf die Spalte angewendet wird. Kann null sein.
        public string? CheckValue { get; set; }

        // Gibt an, ob die Spalte NOT NULL ist. Standardwert ist false.
        public bool? NotNull { get; set; } = false;

        // Gibt an, ob die Spalte ein Primärschlüssel ist. Standardwert ist false.
        public bool IsKey { get; set; } = false;

        // Gibt an, ob die Spalte ein Fremdschlüssel ist. Standardwert ist false.
        public bool IsForeignKey { get; set; } = false;

        // Der Name der Tabelle, auf die der Fremdschlüssel verweist. Standardwert ist ein leerer String.
        public string ReferenceTable { get; set; } = string.Empty;

        // Der Name der Spalte, auf die der Fremdschlüssel verweist. Standardwert ist ein leerer String.
        public string ReferenceColumn { get; set; } = string.Empty;

        // Gibt an, ob die Spalte einen Unique-Constraint hat. Standardwert ist false.
        public bool? IsUnique { get; set; } = false;

        /// <summary>
        /// Converts a `DbColumn` object into a `ColumnDTO` object.
        /// This method maps properties from the `DbColumn` (typically representing a database column)
        /// to the corresponding properties of the `ColumnDTO`.
        /// </summary>
        /// <param name="column">The `DbColumn` object to convert.</param>
        /// <returns>A new `ColumnDTO` object populated with data from the `DbColumn`.</returns>
        public static ColumnDTO ToDTO(DbColumn column)
        {
            return new ColumnDTO
            {
                // Zuweisung der Eigenschaften des DbColumn-Objekts zu den entsprechenden Eigenschaften des ColumnDTO.
                Name = column.Name,
                Type = column.Type,
                IsUnique = column.IsUnique,
                IsKey = column.IsPrimaryKey,
                IsForeignKey = column.IsForeignKey,
                NotNull = !column.IsNull,
                DefaultValue = column.DefaultValue,
                CheckValue = column.CheckConstraint,
                ReferenceColumn = column.ReferenceColumn,
                ReferenceTable = column.ReferenceTable?.Name?? string.Empty,
            };
        }

        public  DbColumn ToDBColum()
        {
            return new DbColumn
            {
                // Zuweisung der Eigenschaften des DbColumn-Objekts zu den entsprechenden Eigenschaften des ColumnDTO.
                Name = this.Name,
                Type = this.Type,
                IsUnique = this.IsUnique??false,
                IsPrimaryKey = this.IsKey,
                IsForeignKey = this.IsForeignKey,
                IsNull = this.NotNull ?? false,
                DefaultValue = this.DefaultValue,
                CheckConstraint = this.CheckValue,
                ReferenceColumn = this.ReferenceColumn,
                ReferenceTable =  new DbTable() { Name = this.ReferenceTable },
            };
        }
    }
}
