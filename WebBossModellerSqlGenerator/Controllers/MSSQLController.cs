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
            StringBuilder sb = new StringBuilder();
            DbDatabase dbDatabase = new DbDatabase(graphicDTO.DatabaseName);
            sb.Append(dbDatabase.ToSqlForMSSSQL());
            sb.Append("USE [" + dbDatabase.Name + "];\n GO \n");
            DbSchema dbSchema = new DbSchema(graphicDTO.SchemaName);
            sb.Append(dbSchema.ToSqlForMSSSQL());
            
            foreach(var elt in graphicDTO.tables)
            {
                DbTable table = new DbTable()
                {
                    Name = elt.Name,
                    Schema = dbSchema
                };
               
                table.Columns = new List<DbColumn>();
                foreach(var col in elt.Columns)
                {
                    DbColumn column = new DbColumn()
                    {
                        Name = col.Name,
                        Type = col.Type,
                        DefaultValue = col.DefaultValue,
                        IsNull = col.IsNullable,
                        IsPrimaryKey = col.IsPrimaryKey,
                        IsUnique = col.IsUnique
                    };
                    table.Columns.Add(column);
                }
               
                sb.Append(table.ToSqlForMSSSQL() +"\n");
                sb.Append(table.AddContrainstsMSSQL() + "\n");
            }

            return Ok(sb.ToString() );
        }
    }
}
