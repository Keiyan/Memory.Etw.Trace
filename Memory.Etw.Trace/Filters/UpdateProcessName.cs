using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RequestData = Microsoft.Diagnostics.EventFlow.Metadata.RequestData;

namespace Memory.Etw.Trace.Filters
{
    public class UpdateProcessName : IFilter
    {
        readonly IHealthReporter _healthReporter;
        readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
        readonly object _syncLock = new object();

        public UpdateProcessName(IHealthReporter healthReporter)
        {
            _healthReporter = healthReporter;
        }

        public FilterResult Evaluate(EventData eventData)
        {
            if (eventData.Payload.TryGetValue(PayloadNames.ProcessId, out var processIdAsObject))
            {
                var processId = (int)processIdAsObject;
                if (!eventData.Payload.TryGetValue(PayloadNames.ProcessName, out var processName) || string.IsNullOrEmpty(processName?.ToString()))
                {
                    eventData.Payload[PayloadNames.ProcessName] = RetrieveProcessName(processId);
                }
                return FilterResult.KeepEvent;
            }
            return FilterResult.DiscardEvent;
        }

        string RetrieveProcessName(int processId)
        {
            if (processId <= 0)
            {
                return string.Empty;
            }
            if (!_memoryCache.TryGetValue<string>(processId, out var name))
            {
                lock (_syncLock)
                {
                    if (!_memoryCache.TryGetValue<string>(processId, out name))
                    {
                        try
                        {
                            name = Process.GetProcessById(processId).ProcessName;
                        }
                        catch
                        {
                            name = string.Empty;
                        }
                        _memoryCache.Set(processId, name, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(1) });
                    }
                }
            }
            return name;
        }

        public class Factory : IPipelineItemFactory<UpdateProcessName>
        {
            public UpdateProcessName CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
            {
                return new UpdateProcessName(healthReporter);
            }
        }
    }
}
