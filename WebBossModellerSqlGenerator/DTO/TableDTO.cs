using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class TableDTO
    {
        public string ClassName {  get; set; }
        public bool IsWeak { get; set; }
        public List<ColumnDTO> Items { get; set; }
        public List<ColumnDTO>? UniquerCombination { get; set; }

        public static TableDTO ToDTO(DbTable table)
        {
            return new TableDTO
            {
                ClassName = table.Name,
                IsWeak = table.IsWeak,
                Items = table.Columns.Select(c=> ColumnDTO.ToDTO(c)).ToList()
            };
        }
    }
}
