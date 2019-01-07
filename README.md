﻿# Memory.Etw.Trace

## Introduction
Memory.Etw.Trace uses the EventFlow library suite and allows to define what diagnostics data to collect, and where they should be outputted to.

## Getting Started
1. Edit "eventFlowConfig.json" and add the Etw events you want to subscribe to:
```
    {
      "type": "ETW",
      "sessionNamePrefix": "Memory.Etw.Trace",
      "cleanupOldSessions": true,
      "reuseExistingSession": true,
      // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/app-domain-resource-monitoring
      // https://docs.microsoft.com/en-us/dotnet/framework/performance/clr-etw-keywords-and-levels
      "providers": [
        {
          // see https://docs.microsoft.com/en-us/dotnet/framework/performance/application-domain-resource-monitoring-arm-etw-events
          "providerName": "Microsoft-Windows-DotNETRuntime",
          "level": "Informational",
          "keywords": "67585" // ThreadingKeyword = 65536 | AppDomainResourceManagementKeyword = 2048 | GCKeyword = 1
        },
        {
          // see https://docs.microsoft.com/en-us/dotnet/framework/performance/loader-etw-events
          // see https://msdn.microsoft.com/cs-cz/library/ff357720(v=vs.100).aspx
          "providerName": "Microsoft-Windows-DotNETRuntimeRundown",
          "level": "Informational",
          "keywords": "65608" // ThreadingKeyword = 65536 | StartRundownKeyword = 64 | LoaderRundownKeyword = 8
        },
        {
          "providerName": "Microsoft-Windows-HttpService",
          "level": "Informational"
        },
		...
      ]
    }
```

2. Still in "eventFlowConfig.json" update the following section to trace the events you are interested in:
```
    {
      "type": "EtwEventTracer",
      "location": "Traces",
      "throttleDuration": "0:00:01",
      "traceAll": false,
      "eventFieldCollectors": {
        // see https://docs.microsoft.com/en-us/dotnet/framework/performance/application-domain-resource-monitoring-arm-etw-events
        "AppDomainResourceManagement/AppDomainMemSurvived": "MemSurvived",

        // see https://docs.microsoft.com/en-us/dotnet/framework/performance/application-domain-resource-monitoring-arm-etw-events
        "AppDomainResourceManagement/AppDomainMemAllocated": "MemAllocated",

        // LoH https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap
        // See https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcheapstats_v1_event
        "GC/HeapStats": "GenerationSize3",

        // See https://docs.microsoft.com/en-us/windows/desktop/http/scenario-2--parser-example-using-etw-tracing
        "HTTPRequestTraceTask/Deliver": "Url"
      }
    }
```

3. Start the program!

## Platform Support
EventFlow supports full .NET Framework (.NET 4.5 series and 4.6 series) and .NET Core, but not all inputs and outputs are supported on all platforms. 
The following table lists platform support for standard inputs and outputs.  

| Input Name | .NET 4.5.1 | .NET 4.6 | .NET Core |
| :------------ | :---- | :---- | :---- |
| *Inputs* |
| [System.Diagnostics.Trace](#trace) | Yes | Yes | Yes |
| [EventSource](#eventsource) | No | Yes | Yes |
| [PerformanceCounter](#performancecounter) | Yes | Yes | No |
| [Serilog](#serilog) | Yes | Yes | Yes |
| [Microsoft.Extensions.Logging](#microsoftextensionslogging) | Yes | Yes | Yes |
| [ETW (Event Tracing for Windows)](#etw-event-tracing-for-windows) | Yes | Yes | No |
| [Application Insights input](#application-insights-input) | Yes | Yes | Yes |
| *Outputs* |
| [StdOutput (console output)](#stdoutput) | Yes | Yes | Yes |
| [Application Insights](#application-insights) | Yes | Yes | Yes |
| [Azure EventHub](#event-hub) | Yes | Yes | Yes |
| [Elasticsearch](#elasticsearch) | Yes | Yes | Yes |
| [OMS (Operations Management Suite)](#oms-operations-management-suite) | Yes | Yes | Yes |
