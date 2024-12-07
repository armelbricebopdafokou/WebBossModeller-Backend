namespace WebBossModellerSqlGenerator.DTO
{
    public class TableDTO
    {
        public string Name {  get; set; }
        public bool IsWeak { get; set; }
        public List<ColumnDTO> Columns { get; set; }
        public List<ColumnDTO> UniquerCombination { get; set; }
    }
}
