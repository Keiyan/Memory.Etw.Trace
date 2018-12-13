using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory.Etw.Trace.Inputs
{
    public class EtwInputConfigurationPatch : EtwInput
    {
        public EtwInputConfigurationPatch(IConfiguration configuration, IHealthReporter healthReporter)
            : base(configuration, healthReporter)
        {

        }
    }
}
