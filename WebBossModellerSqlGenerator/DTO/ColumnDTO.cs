

using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class ColumnDTO
    {
        public string Name { get; set; }
        public string? Type { get; set; }
        public string? DefaultValue { get; set; }
        public string? CheckValue {get; set;}
        public bool? NotNull { get; set; } = false;
        public bool IsKey { get; set; } = false;
        public bool? IsUnique { get; set; } = false;

        public static ColumnDTO ToDTO(DbColumn column)
        {
            return new ColumnDTO
            {
                Name = column.Name,
                Type = column.Type,
                IsUnique = column.IsUnique,
                IsKey = column.IsPrimaryKey,
                NotNull = !column.IsNull,
                DefaultValue = column.DefaultValue,
                CheckValue = column.CheckConstraint

            };
        }

    }
}
