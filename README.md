# Objective

**Find the bug(s) !**

We want to capture the memory allocation and deallocation at the AppDomain level on existing or future processes.
We want to be able to filter monitored processes by their name.

## Event Tracing for Windows

[ETW (Event Tracing for Windows)](https://docs.microsoft.com/en-us/windows/desktop/etw/event-tracing-portal) provides low level monitoring as structured events.
More documentation on the usage of ETW can be found further on in this document. You will also find pointers on relevant documentation directly in the source code

## Some basics about memory

First, the fundamentals, **the ```new``` command does not allocate physical memory**.
All that it does is reserving a part of the process address space (which is made of all addresses from 0x0 to 0xFF...F) and convey a promise from the Operating System to give actual memory when needed.
It is only when one byte of this address space is actually accessed (be there for reading or writing) that actual memory allocation takes place.

Besides, the process address space is not the real address space. Process address 0x1234 (real number are 32 or 64-bit long !), for example, is translated by the OS to
physical address 0x5678. And 0x5678 is not constant, because RAM can be paged in or out to swap file.

Second, some .Net specific

In .Net, you have two different types of memory: *stack* and *heap*. Basically, stack is for static allocations (everything not allocated by ```new```) 
and heap for dynamic one (everything allocated by ```new```). There are more subtelties to that, but this basic understanding is more than enough for this case.

Regarding heap, there are in reality two different heaps: SOH and LOH. SOH stands for Small Object Heap and LOH for Large Object Heap.
Every object larger than 85 000 bytes is stored on the LOH

The main difference between theses two heaps is compaction: when an object in the SOH is freed, the SOH is compacted, meaning there is no 'hole' in the SOH.
LOH, because it's made of large objects that can take time to move, is never compacted. When an object is freed, it leaves a hole that can be reused in a later large allocation

Third, about the Garbage Collector

The .Net garbage collector organize objects in *generations*. Objects are created in Gen0 and, if they survive one collection, are promoted to Gen1, and then
further up to Gen2 it they survive more. Of course, Gen0 collections are more frequent than Gen1 (and Gen2) and thus, short lived objects are quickly collected
while long lived one are only touched from time to time.

One exception, however, is that objects created on the LOH are directly placed in Gen2. Large object are expensive to collect because of their size, so,
by placing them in Gen2, the GC ensures that they are not sweeped too often. This may be reffered as Gen3 in some spaces.

# Problem

## What does this program do exactly

We aim at capturing three different informations:
* The current size of the LOH
* How many bytes each AppDomain allocates
* How many bytes eahc AppDomain frees

## Bug report

As it is written, this program only captures the LOH size. The two latter informations are never captured.
Your objective is to find out why and to correct the program so that it works as intended

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
