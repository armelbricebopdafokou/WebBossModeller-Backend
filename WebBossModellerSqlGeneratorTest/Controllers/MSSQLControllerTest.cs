using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebBossModellerSqlGenerator.Controllers;

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
    }
}
