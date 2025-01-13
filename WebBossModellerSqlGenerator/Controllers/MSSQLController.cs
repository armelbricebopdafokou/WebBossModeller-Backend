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
        

        public MSSQLController()
        {
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
    }
}
