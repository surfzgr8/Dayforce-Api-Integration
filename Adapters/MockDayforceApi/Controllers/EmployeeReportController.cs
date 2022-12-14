using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IVCE.DAI.Adapters.Models;

namespace MockDayforceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeReportController : ControllerBase
    {



        private EmployeeChangesReport employeeChangesReports = new EmployeeChangesReport
        {
            Data = new Data
            {
                XRefCode = "API_Test_1",
                Rows = new Row[]
                {
                   new Row{ EmployeeCommonName="Mr TestTube1",EmployeeFirstName="Testy" ,EmployeeLastName="TestTube1",},
                   new Row{ EmployeeCommonName="Mr Testicular",EmployeeFirstName="Tozzer" ,EmployeeLastName="Testy2"}
                }
            }
        };

        private CanonicalAADUserItem CanonicalAADUser = new CanonicalAADUserItem
        {
            Header = new Header
            {
                ApplicationId="DAI",
                OperationStatus="NEW",
                XRefCode= "API_Test_1"
            },
            AADUserItem=new Item
            {
                EmployeeId="1234567890",
                FirstName="Paul",
                LastName="Massen",
                EmployeeKnownAsFirstName="Pauly"
            }
        };

        private readonly ILogger<EmployeeReportController> _logger;

        public EmployeeReportController(ILogger<EmployeeReportController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public EmployeeChangesReport Get()
        {
            var rng = new Random();

            return this.employeeChangesReports;

            //return new EmployeeChangesReport[]
            //    {
            //        new EmployeeChangesReport{EmployeeCommonName="test1",EmployeeEmployeeId=1},
            //        new EmployeeChangesReport{EmployeeCommonName="test2",EmployeeEmployeeId=2}
            //    };
        }

        [HttpGet]
        [Route("CanonicalAADUser")]
        public CanonicalAADUserItem GetAADUSer()
        {
            var rng = new Random();

            return this.CanonicalAADUser;

            //return new EmployeeChangesReport[]
            //    {
            //        new EmployeeChangesReport{EmployeeCommonName="test1",EmployeeEmployeeId=1},
            //        new EmployeeChangesReport{EmployeeCommonName="test2",EmployeeEmployeeId=2}
            //    };
        }
    }
}
