using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebBossModellerSqlGenerator.Controllers;

namespace WebBossModellerSqlGeneratorTest.Controllers
{
    public class PostgresControllerTest
    {
        [Fact]
        public async Task GetStatementCreateDatabase()
        {
            //Arrange
            var postgresController = new PostgresSQLController();
            var dbname = "";
            //Act
            var result = await postgresController.CreateDatabase(dbname);
            //Assert
            Assert.False(result is OkObjectResult);
        }

        [Fact]
        public async Task GetStatementCreateDatabaseWithCaseSensitive()
        {
            //Arrange
            var postgresController = new PostgresSQLController();
            var dbname = "Test";
            //Act
            var result = await postgresController.CreateDatabase(dbname, true);
            var resultOk = result as OkObjectResult;
            var str = resultOk.Value as string;
            //Assert
            Assert.NotNull(str);
        }
    }
}
