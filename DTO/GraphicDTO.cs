using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class GraphicDTO
    {
        public string DatabaseName { get; set; }
        public string SchemaName { get; set; }
        public TableDTO[] tables { get; set; }
        public bool IsCaseSensitive { get; set; }
    }
}
