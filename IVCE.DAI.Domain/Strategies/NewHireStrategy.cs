using IVCE.DAI.Domain.Models.Canonical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVCE.DAI.Domain.Strategies
{
    public class NewHireStrategy : IStrategy
    {
        public bool? Validate(IList<CanonicalWorkerItem> canonicalAADUserItemList)
        {
            // is this a New Hire that has never been process byt this system and does not exist in the eventstore
            return
                   (canonicalAADUserItemList.LastOrDefault().WorkerItem?.EmploymentStatusReasonCode) != "NEWHIRE" ? null : true;
                //canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Inserted";
            


        }
    }
}
