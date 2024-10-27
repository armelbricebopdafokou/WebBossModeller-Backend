using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MSSQLController : ControllerBase
    {
        private ILogger _logger;
        public MSSQLController(ILogger<MSSQLController> logger)
        {
            _logger = logger;
        }

        public MSSQLController()
        {
        }

        [HttpGet]
        public async Task<IActionResult> CreateDatabase(string dbName)
        {
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");
            DbDatabase db = new DbDatabase(dbName);
            
            return Ok(JsonConvert.SerializeObject(db.ToSqlForMSSSQL())) ;
        }

        [HttpGet]
        public async Task<IActionResult> CreateSchema(string dbSchema)
        {
            if (string.IsNullOrWhiteSpace(dbSchema))
                return BadRequest("Parameter Name is missing");
            DbSchema db = new DbSchema(dbSchema);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForMSSSQL()));
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSqlForGraphic(GraphicDTO graphicDTO)
        {
            StringBuilder sb = new StringBuilder();
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb.Append(dbDatabase.ToSqlForMSSSQL());
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb.Append(dbSchema.ToSqlForMSSSQL());
            sb.Append("USE "+ dbSchema.Name + "\n");
            foreach(var elt in graphicDTO.tables)
            {
                sb.Append(elt.ToSqlForMSSSQL());
            }
            return Ok(sb);
        }
    }
}
