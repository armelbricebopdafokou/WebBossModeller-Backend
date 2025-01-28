using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class MySqlController : ControllerBase
    {
        private readonly MySQLDatabaseService _mysqlService;
        public MySqlController(MySQLDatabaseService mysqlService)
        {
            _mysqlService = mysqlService;
        }

        [HttpGet]
        public ActionResult<string> CreateDatabase(string dbName)
        {
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");
            DbDatabase db = new DbDatabase(dbName);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForMySQL()));
        }

        [HttpGet]
        public ActionResult<string> CreateSchema(string dbSchema)
        {
            if (string.IsNullOrWhiteSpace(dbSchema))
                return BadRequest("Parameter Name is missing");
            DbSchema db = new DbSchema(dbSchema);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForMySQL()));
        }

        [HttpPost]
        public ActionResult<string> GenerateSqlForGraphic([FromBody] GraphicDTO graphicDTO)
        {
            StringBuilder sb = new StringBuilder();
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb.Append(dbDatabase.ToSqlForMySQL());
            sb.Append("USE [" + dbDatabase.Name + "];\n ");
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb.Append(dbSchema.ToSqlForMySQL());

            foreach (var elt in graphicDTO.tables)
            {
                DbTable table = new DbTable()
                {
                    Name = elt.ClassName,
                    Schema = dbSchema
                };

                table.Columns = new List<DbColumn>();
                foreach (var col in elt.Items)
                {
                    DbColumn column = new DbColumn()
                    {
                        Name = col.Name,
                        Type = col.Type,
                        DefaultValue = col.DefaultValue,
                        IsNull = !(col.NotNull??false),
                        IsPrimaryKey = col.IsKey,
                        IsUnique = col.IsUnique??false
                    };
                    table.Columns.Add(column);
                }

                sb.Append(table.ToSqlForMySQL() + "\n");
                sb.Append(table.AddContrainstsMySQL() + "\n \n \n");
            }

            return Ok(sb.ToString());
        }

        [HttpGet]
        public ActionResult<string> GetDiagramFromDatabase(string host,string dbName, string schemaName, string dbUser, string dbPassword)
        {
             if (string.IsNullOrWhiteSpace(host))
                return BadRequest("Parameter Hostname is missing");
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Database Name is missing");
            if (string.IsNullOrWhiteSpace(schemaName))
                return BadRequest("Parameter Schema Name is missing");
            if (string.IsNullOrWhiteSpace(dbPassword))
                return BadRequest("Parameter user password is missing");
            if (string.IsNullOrWhiteSpace(dbUser))
                return BadRequest("Parameter User Name is missing");
            
            try
            {
                string connectionString = $"server={host};user={dbUser};database={dbName};password={dbPassword};";
                var db = _mysqlService.GetDatabaseSchema(connectionString, schemaName);
                 var dbDTO = DatabaseDTO.ToDTO(db);
                return Ok(JsonConvert.SerializeObject(dbDTO));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

    }
}
