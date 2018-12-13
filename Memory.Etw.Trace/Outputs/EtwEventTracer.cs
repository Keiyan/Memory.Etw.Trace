using Memory.Etw.Trace.Utils;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace Memory.Etw.Trace.Outputs
{
    public partial class EtwEventTracer : IOutput, IDisposable, IRequireActivation
    {
        const string TraceAllFileName = "_TraceAll";
        const string UniqueEventNameFileName = "_UniqueEventNames";
        const string TraceFileExtension = ".csv";

        readonly IHealthReporter _healthReporter;
        readonly IDictionary<string, (FileStream Stream, TimeSpanThrottle Throttle)> _streams = new Dictionary<string, (FileStream, TimeSpanThrottle)>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _uniqueEventNames = new HashSet<string>();
        readonly object _syncLock = new object();

        public EtwEventTracer(IConfiguration configuration, IHealthReporter healthReporter)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _healthReporter = healthReporter ?? throw new ArgumentNullException(nameof(healthReporter));
            Location = configuration.GetValue<string>(nameof(Location)) ?? ".";
            TraceAll = configuration.GetValue<bool?>(nameof(TraceAll)) ?? false;
            var throttle = configuration.GetValue<string>(nameof(ThrottleDuration)) ?? "-0:00:01";
            try
            {
                ThrottleDuration = TimeSpan.Parse(throttle, CultureInfo.InvariantCulture);
            }
            catch
            {
                _healthReporter.ReportProblem($"{nameof(EtwEventTracer)}: throttle duration is invalid", EventFlowContextIdentifiers.Configuration);
                ThrottleDuration = TimeSpan.FromSeconds(1);
            }
            var providersConfiguration = configuration.GetSection(nameof(EventFieldCollectors));
            if (providersConfiguration == null)
            {
                healthReporter.ReportProblem($"{nameof(EtwEventTracer)}: required configuration section '{nameof(EventFieldCollectors)}' is missing");
                return;
            }
            try
            {
                providersConfiguration.Bind(EventFieldCollectors);
            }
            catch
            {
                _healthReporter.ReportProblem($"{nameof(EtwEventTracer)}: configuration is invalid", EventFlowContextIdentifiers.Configuration);
            }
        }

        public string Location { get; } = ".";
        public TimeSpan ThrottleDuration { get; }
        public bool TraceAll { get; }
        public SortedDictionary<string, string> EventFieldCollectors { get; set; } = new SortedDictionary<string, string>();
        string UniqueEventNamePath => Path.Combine(Location, UniqueEventNameFileName + TraceFileExtension);

        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            foreach (var eventData in events)
            {
                if (eventData.Payload.TryGetValue("EventName", out var eventName) && eventName != null &&
                    eventData.Payload.TryGetValue(PayloadNames.ProcessId, out var processId))
                {
                    TraceEventInTraceAll(eventData);
                    TraceUniqueEventName((string)eventName);

                    if (EventFieldCollectors.TryGetValue((string)eventName, out var field) && field != null &&
                        eventData.Payload.TryGetValue(field, out var value))
                    {
                        TraceEvent(eventData, processId, field, value);
                    }
                }
            }
            return Task.CompletedTask;
        }

        void LoadUniqueEventNames()
        {
            try
            {
                if (File.Exists(UniqueEventNamePath))
                {
                    foreach (var line in File.ReadLines(UniqueEventNamePath))
                    {
                        _uniqueEventNames.Add(line);
                    }
                }
            }
            catch
            {
            }
        }


        (FileStream Stream, TimeSpanThrottle Throttle) GetStream(string field, string header = null)
        {
            lock (_syncLock)
            {
                if (!_streams.TryGetValue(field, out var result))
                {
                    Directory.CreateDirectory(Location);
                    var path = Path.Combine(Location, field + TraceFileExtension);
                    var newFile = !File.Exists(path);
                    _streams[field] = result = (new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite), new TimeSpanThrottle(ThrottleDuration));
                    if (newFile && !string.IsNullOrEmpty(header))
                    {
                        AppendToStream(result.Stream, header);
                    }
                }
                return result;
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                foreach (var data in _streams.Values)
                {
                    data.Stream.Dispose();
                }
                _streams.Clear();
            }
        }

        public void Activate()
        {
            if (!EventFieldCollectors.Any())
            {
                _healthReporter.ReportWarning($"{nameof(EtwEventTracer)}: no event field collectors.", EventFlowContextIdentifiers.Configuration);
            }
            LoadUniqueEventNames();
        }

        public class Factory : IPipelineItemFactory<EtwEventTracer>
        {
            public EtwEventTracer CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
            {
                return new EtwEventTracer(configuration, healthReporter);
            }
        }
    }
}
