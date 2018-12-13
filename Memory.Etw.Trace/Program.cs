using Microsoft.Diagnostics.EventFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory.Etw.Trace
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
            {
                System.Diagnostics.Trace.TraceInformation("EventFlow is working!");
                Console.WriteLine("Collecting events. Press any key to exit...");
                Console.ReadKey(intercept: true);
            }
        }
    }
}
