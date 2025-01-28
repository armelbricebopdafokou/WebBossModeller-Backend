using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class MSSQLController : ControllerBase
    {
        private readonly MSSQLDatabaseService _mssqlService;

        public MSSQLController( MSSQLDatabaseService mssqlService)
        {
             _mssqlService = mssqlService;
        }

        [HttpGet]
        public ActionResult CreateDatabase(string dbName)
        {
            if (string.IsNullOrWhiteSpace(dbName))
                return BadRequest("Parameter Name is missing");
            DbDatabase db = new DbDatabase(dbName);
            
            return Ok(JsonConvert.SerializeObject(db.ToSqlForMSSSQL())) ;
        }

        [HttpGet]
        public IActionResult CreateSchema(string dbSchema)
        {
            if (string.IsNullOrWhiteSpace(dbSchema))
                return BadRequest("Parameter Name is missing");
            DbSchema db = new DbSchema(dbSchema);

            return Ok(JsonConvert.SerializeObject(db.ToSqlForMSSSQL()));
        }

        [HttpPost]
        public  ActionResult<string> GenerateSqlForGraphic([FromBody]GraphicDTO graphicDTO)
        {
            string sb = string.Empty;
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb += dbDatabase.ToSqlForMSSSQL();
            sb += "USE [" + dbDatabase.Name + "];\n GO \n";
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb += dbSchema.ToSqlForMSSSQL();
            
            foreach(var elt in graphicDTO.tables)
            {
                DbTable table = new DbTable()
                {
                    Name = elt.ClassName,
                    Schema = dbSchema
                };
               
                table.Columns = new List<DbColumn>();
                foreach(var col in elt.Items)
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
               
                sb += table.ToSqlForMSSSQL() +"\n";
                sb += table.AddContrainstsMSSQL() + "\n \n \n";
            }

            return Ok(sb );
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
                string connectionString = $"Data Source={host};Initial Catalog={dbName};User Id={dbUser};Password={dbPassword};";
                var db = _mssqlService.GetDatabaseSchema(connectionString, schemaName);
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
