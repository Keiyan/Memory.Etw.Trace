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
    public partial class EtwEventTracer
    {
        void TraceEventInTraceAll(EventData eventData)
        {
            if (TraceAll)
            {
                lock (_syncLock)
                {
                    var data = string.Join("\n", eventData.Payload.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    var info = GetStream(TraceAllFileName);
                    AppendToStream(info.Stream, data + "\n--------");
                }
            }
        }

        void TraceUniqueEventName(string eventName)
        {
            lock (_uniqueEventNames)
            {
                if (_uniqueEventNames.Contains(eventName)) return;
                var info = GetStream(UniqueEventNameFileName);
                AppendToStream(info.Stream, eventName);

                _uniqueEventNames.Add(eventName);
            }
        }

        void TraceEvent(EventData e, object processId, string field, object value)
        {
            try
            {
                lock (_syncLock)
                {
                    var info = GetStream(field, "Timestamp;ProcessId;Process;AppDomain;Value");
                    var throttle = value.GetType().IsValueType;
                    if (throttle)
                    {
                        info.Throttle.Execute(() => TraceEventImpl(e, processId, value, info.Stream));
                    }
                    else
                    {
                        TraceEventImpl(e, processId, value, info.Stream);
                    }
                }
            }
            catch (Exception ex)
            {
                _healthReporter.ReportProblem(ex.Message, nameof(EtwEventTracer));
            }
        }

        static void TraceEventImpl(EventData e, object processId, object value, FileStream stream)
        {
            object processInfo;
            if (!e.Payload.TryGetValue(PayloadNames.ProcessName, out processInfo) || string.IsNullOrEmpty(processInfo?.ToString()))
            {
                processInfo = processId;
            }

            var message = Invariant($"{e.Timestamp};{processId};{processInfo};{GetAppDomainInfo(e)};{value}");
            AppendToStream(stream, message);
        }

        static object GetAppDomainInfo(EventData eventData)
        {
            if (eventData.Payload.TryGetValue(PayloadNames.AppDomainName, out var name) && !string.IsNullOrEmpty(name?.ToString()))
            {
                return name.ToString();
            }
            if (eventData.Payload.TryGetValue(PayloadNames.AppDomainId, out var id))
            {
                return id;
            }

            return string.Empty;
        }

        static void AppendToStream(FileStream stream, string message)
        {
            stream.Seek(0, SeekOrigin.End);
            using (var writer = new StreamWriter(stream, Encoding.Default, 1024, leaveOpen: true))
            {
                writer.WriteLine(message);
            }
        }
    }
}