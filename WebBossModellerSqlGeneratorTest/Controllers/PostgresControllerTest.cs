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
        public void GetStatementCreateDatabase()
        {
            //Arrange
            var postgresController = new PostgresSQLController();
            var dbname = "";
            //Act
            var result =  postgresController.CreateDatabase(dbname);
            //Assert
            Assert.False(result is OkObjectResult);
        }

        [Fact]
        public void GetStatementCreateDatabaseWithCaseSensitive()
        {
            //Arrange
            var postgresController = new PostgresSQLController();
            var dbname = "Test";
            //Act
            var result =  postgresController.CreateDatabase(dbname, true);
            var resultOk = result.Result as OkObjectResult;
            var str = resultOk?.Value as string;
            //Assert
            Assert.NotNull(str);
        }
    }
}
