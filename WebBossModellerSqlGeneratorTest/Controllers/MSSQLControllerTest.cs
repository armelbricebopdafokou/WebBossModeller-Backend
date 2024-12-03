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
            var result =  mssqlController.CreateDatabase(db);
            var res = result as OkObjectResult;
            //Assert
            Assert.NotNull(res);

           
        }

        [Fact]
        public async Task CreateDatabaseTest2()
        {
            //Arrange
            var db = "";
            var result =  mssqlController.CreateDatabase(db);
            //Act
           
            //Assert
            Assert.True(result is BadRequestObjectResult);
        }

        [Fact]
        public void GetQuery()
        {
            //Arrange
            GraphicDTO graphicDTO = new GraphicDTO()
            {
                DatabaseName = "test",
                SchemaName = "mySchema",
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
                                IsNullable=true,
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

            //Assert
            Exception ex = Assert.Throws<Exception>(() => mssqlController.GenerateSqlForGraphic(graphicDTO));
            Assert.Equal("A primary key must not be null", ex.Message);
        }


        [Fact]
        public void GetQuery2()
        {
            //Arrange
            GraphicDTO graphicDTO = new GraphicDTO()
            {
                DatabaseName = "test",
                SchemaName = "mySchema",
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
                                Name = "userMatricule",
                                Type = "bigint",
                                IsPrimaryKey = true,
                                IsNullable=false,
                            },
                             new ColumnDTO()
                            {
                                Name = "email",
                                Type = "nvarchar(500)",
                                IsUnique=true,
                            },

                            new ColumnDTO()
                            {
                                Name = "lastName",
                                Type = "nvarchar(500)",
                                IsNullable=false,
                            }
                        },
                        UniquerCombination = new List<ColumnDTO>()
                        {
                            new ColumnDTO()
                            {
                                Name = "firstName",
                                Type = "nvarchar(500)",
                                IsNullable = false,
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
            var result = mssqlController.GenerateSqlForGraphic(graphicDTO);
            var okResult = result.Result as OkObjectResult;

            

            //Assert
            Assert.NotNull(okResult?.Value);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
