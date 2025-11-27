using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestMonitoringLibrary.Enitites.Domain;

public enum DomainStatusType
{
    Allowed, // 200
    Forbidden, // 403
    Greylisted // 402
}
