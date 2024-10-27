using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PostgresSQLController : ControllerBase
    {
        public PostgresSQLController()
        {
           
        }

        [HttpGet]
        public async Task<IActionResult> CreateDatabase(string dbName, bool isCaseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");
            DbDatabase db = new DbDatabase(dbName);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForPostgresSQL(isCaseSensitive)));
        }

        [HttpGet]
        public async Task<IActionResult> CreateSchema(string dbSchema, bool isCaseSensitive=false)
        {
            if (string.IsNullOrWhiteSpace(dbSchema))
                return BadRequest("Parameter Name is missing");
            DbSchema db = new DbSchema(dbSchema);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForPostgresSQL(isCaseSensitive)));
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSqlForGraphic(GraphicDTO graphicDTO, bool isCaseSensitive=false)
        {
            StringBuilder sb = new StringBuilder();
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb.Append(dbDatabase.ToSqlForMSSSQL());
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb.Append(dbSchema.ToSqlForPostgresSQL(isCaseSensitive));
            sb.Append("USE " + dbSchema.Name + "\n");
            foreach (var elt in graphicDTO.tables)
            {
                sb.Append(elt.ToSqlForPostgresSQL(isCaseSensitive));
            }
            return Ok(sb);
        }

    }
}
