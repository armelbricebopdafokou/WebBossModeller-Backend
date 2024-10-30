using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebBossModellerSqlGenerator.Controllers;
using WebBossModellerSqlGenerator.DTO;

namespace WebBossModellerSqlGeneratorTest.Controllers
{
    public class MSSQLControllerTest
    {
        MSSQLController mssqlController;
        public MSSQLControllerTest()
        {
            mssqlController = new MSSQLController();
        }

        [Fact]
        public async Task CreateDatabaseTest()
        {
            //Arrange
            var db = "sopra";
            //Act
            var result = await mssqlController.CreateDatabase(db);
            var res = result as OkObjectResult;
            //Assert
            Assert.NotNull(res);

           
        }

        [Fact]
        public async Task CreateDatabaseTest2()
        {
            //Arrange
            var db = "";
            var result = await mssqlController.CreateDatabase(db);
            //Act
           
            //Assert
            Assert.True(result is BadRequestObjectResult);
        }

        [Fact]
        public async Task GetQuery()
        {
            //Arrange
            GraphicDTO graphicDTO = new GraphicDTO()
            {
                DatabaseName= "test",
                SchemaName= "mySchema",
                tables = new TableDTO[]
                {
                    new TableDTO()
                    {
                        IsWeak = false,
                        Name = "users",
                        Columns = new List<ColumnDTO>()
                        {
                            new ColumnDTO()
                            {
                                Name = "firstName",
                                Type = "nvarchar(500)",
                                IsUnique = true,
                                IsNullable = false,
                            },
                            new ColumnDTO()
                            {
                                Name = "userId",
                                Type = "bigint",
                                IsPrimaryKey = true,
                                IsNullable=false,
                            },
                             new ColumnDTO()
                            {
                                Name = "lastName",
                                Type = "nvarchar(500)",
                                IsNullable=false,
                            }
                        }
                    }
                }
            };

            //Act
            var result = await mssqlController.GenerateSqlForGraphic(graphicDTO);
            var sql = result as OkObjectResult;
             

            //Assert
            Assert.NotNull(sql.Value);
        }
    }
}
