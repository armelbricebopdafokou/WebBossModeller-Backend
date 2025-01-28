using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class PostgresSQLController : ControllerBase
    {
        private readonly PostgreSQLDatabaseService _postgresqlService;
        public PostgresSQLController(PostgreSQLDatabaseService postgresqlService)
        {
           _postgresqlService = postgresqlService;
        }

        [HttpGet]
        public ActionResult<string> CreateDatabase(string dbName, bool isCaseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");
            DbDatabase db = new DbDatabase(dbName);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForPostgresSQL(isCaseSensitive)));
        }

        [HttpGet]
        public ActionResult<string> CreateSchema(string dbSchema, bool isCaseSensitive=false)
        {
            if (string.IsNullOrWhiteSpace(dbSchema))
                return BadRequest("Parameter Name is missing");
            DbSchema db = new DbSchema(dbSchema);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForPostgresSQL(isCaseSensitive)));
        }

        [HttpPost]
        public ActionResult<string> GenerateSqlForGraphic([FromBody]GraphicDTO graphicDTO)
        {
            bool isCaseSensitive= graphicDTO.IsCaseSensitive;
            StringBuilder sb = new StringBuilder();
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb.Append(dbDatabase.ToSqlForPostgresSQL(isCaseSensitive));
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb.Append(dbSchema.ToSqlForPostgresSQL(isCaseSensitive));
            if (isCaseSensitive == true)
                sb.Append("SET search_path TO \"" + dbSchema.Name + "\" \n");
            else
                sb.Append("SET search_path TO " + dbSchema.Name + "\n");
            foreach (var elt in graphicDTO.tables)
            {
                // sb.Append(elt.ToSqlForPostgresSQL(isCaseSensitive));
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

                sb.Append(table.ToSqlForPostgresSQL(isCaseSensitive) + "\n");
                sb.Append(table.AddContrainstsPostgres(isCaseSensitive) + "\n \n \n");
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
                string connectionString = $"User ID={dbUser};Password={dbPassword};Host={host};Port=5432;Database={dbName};";
                var db = _postgresqlService.GetDatabaseSchema(connectionString, schemaName);
                var dbDTO = DatabaseDTO.ToDTO(db);
                return Ok(JsonConvert.SerializeObject(dbDTO));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    


    }
}
