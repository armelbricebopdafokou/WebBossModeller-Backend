namespace WebBossModellerSqlGenerator.DTO
{
    public class ColumnDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsNullable { get; set; } = false;
        public bool IsPrimaryKey { get; set; } = false;
        public bool IsUnique { get; set; } = false;
    }
}
