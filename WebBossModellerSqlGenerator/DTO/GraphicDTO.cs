using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class GraphicDTO
    {
        public string DatabaseName { get; set; }
        public string SchemaName { get; set; }
        public DbTable[] tables { get; set; }
    }
}
