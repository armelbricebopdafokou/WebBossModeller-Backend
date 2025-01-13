namespace WebBossModellerSqlGenerator.DTO
{
    public class TableDTO
    {
        public string ClassName {  get; set; }
        public bool IsWeak { get; set; }
        public List<ColumnDTO> Items { get; set; }
        public List<ColumnDTO>? UniquerCombination { get; set; }
    }
}
