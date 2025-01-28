using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class DatabaseDTO
    {
        public string Name {  get; set; }
        public List<SchemaDTO> Schemas { get; set; }

        public static DatabaseDTO ToDTO(DbDatabase dbDatabase)
        {
            return new DatabaseDTO
            {
                Name = dbDatabase.Name,
                Schemas = dbDatabase.Schemas.Select(s=> SchemaDTO.ToDTO(s)).ToList()
            };
        }
    }
}
