using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebBossModellerSqlGenerator.DTO;
using WebBossModellerSqlGenerator.Models;

namespace WebBossModellerSqlGenerator.Controllers
{
    public class MySqlController : Controller
    {

        public MySqlController()
        {
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
    }
}
