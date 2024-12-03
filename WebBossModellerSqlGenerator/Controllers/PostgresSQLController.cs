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
        public PostgresSQLController()
        {
           
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
                    Name = elt.Name,
                    Schema = dbSchema
                };

                table.Columns = new List<DbColumn>();
                foreach (var col in elt.Columns)
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

                sb.Append(table.ToSqlForPostgresSQL(isCaseSensitive) + "\n");
                sb.Append(table.AddContrainstsPostgres(isCaseSensitive) + "\n");
            }
            return Ok(sb.ToString());
        }

    }
}
