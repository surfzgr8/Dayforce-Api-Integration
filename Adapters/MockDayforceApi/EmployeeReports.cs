using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IVCE.DAI.Adapters.Models;


namespace MockDayforceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeReportsController : ControllerBase
    {
        private static readonly string[] EmployeeChangeReports = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        EmployeeChangesReport[] employeeChangesReports = new EmployeeChangesReport[]
        {
            new EmployeeChangesReport{EmployeeCommonName="test1",EmployeeEmployeeId=1},
            new EmployeeChangesReport{EmployeeCommonName="test1",EmployeeEmployeeId=1}
        };



        private readonly ILogger<EmployeeReportsController> _logger;

        public EmployeeReportsController(ILogger<EmployeeReportsController> logger)
        {
            _logger = logger;
        }

        [Route("GetEmployeeChangesReport")]
        [HttpPost]
        public IEnumerable<EmployeeChangesReport> GetEmployeeChangesReport()
        {
            return employeeChangesReports.ToList();

            //   HttpContext.VerifyUserHasAnyAcceptedScope(_scopesRequiredByAPI);

            //    var oMedUnitsList = await _oMedsRepository.GetOMedUnitsAsync(getOMedUnitsRequest);

            //    foreach (var oMedUnitResult in oMedUnitsList)
            //    {
            //        yield return oMedUnitResult;
            //    }
        }
    }
}
