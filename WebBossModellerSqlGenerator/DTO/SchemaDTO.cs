using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.DTO
{
    public class SchemaDTO
    {
        public string Name {  get; set; }
        public List<TableDTO> Keys { get; set; }

        public static SchemaDTO ToDTO(DbSchema schema)
        {
            return new SchemaDTO
            {
                Name = schema.Name,
                Keys = schema.Tables.Select(t=> TableDTO.ToDTO(t)).ToList()
            };
        }
    }
}
