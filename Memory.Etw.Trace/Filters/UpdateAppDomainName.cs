using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory.Etw.Trace.Filters
{
    public class UpdateAppDomainName : IFilter
    {
        const ushort AppDomainLoad = 156;
        const ushort AppDomainDCStartEventId = 157;

        readonly IDictionary<int, IDictionary<long, string>> _processAppDomainMapping = new SortedDictionary<int, IDictionary<long, string>>();
        readonly object _syncLock = new object();

        public FilterResult Evaluate(EventData eventData)
        {
            if (eventData.Payload.TryGetValue(PayloadNames.ProcessId, out var processId))
            {
                UpdateMapping(eventData, (int)processId);
                UpdateAppDomainNameFromMapping(eventData, (int)processId);
            }
            return FilterResult.KeepEvent;
        }
        
        void UpdateMapping(EventData eventData, int processId)
        {
            if (eventData.Payload.TryGetValue(PayloadNames.EventId, out var eventIdAsObject) && eventIdAsObject is IConvertible eventIdAsConvertible)
            {
                var eventId = eventIdAsConvertible.ToUInt16(CultureInfo.InvariantCulture);
                // Update appdomain name info when it gets started
                if (eventId == AppDomainDCStartEventId || eventId == AppDomainLoad)
                {
                    var appDomainId = (long)eventData.Payload[PayloadNames.AppDomainId];
                    var appDomainName = (string)eventData.Payload[PayloadNames.AppDomainName];

                    GetProcessingMapping(processId)[appDomainId] = appDomainName;
                }
            }
        }

        void UpdateAppDomainNameFromMapping(EventData eventData, int processId)
        {
            if (eventData.Payload.TryGetValue(PayloadNames.AppDomainId, out var appDomainId) &&
                !eventData.Payload.TryGetValue(PayloadNames.AppDomainName, out var ignored))
            {
                var appDomainMapping = GetProcessingMapping(processId);
                if (appDomainMapping.TryGetValue((long)appDomainId, out var appDomainName))
                {
                    eventData.Payload[PayloadNames.AppDomainName] = appDomainName;
                }
            }
        }

        IDictionary<long, string> GetProcessingMapping(int processId)
        {
            lock (_syncLock)
            {
                if (!_processAppDomainMapping.TryGetValue(processId, out var result))
                {
                    _processAppDomainMapping[processId] = result = new SortedDictionary<long, string>();
                }
                return result;
            }
        }

        public class Factory : IPipelineItemFactory<UpdateAppDomainName>
        {
            public UpdateAppDomainName CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
            {
                return new UpdateAppDomainName();
            }
        }
    }
}
