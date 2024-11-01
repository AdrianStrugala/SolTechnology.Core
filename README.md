

<p align="center">
    <img alt="SolTechnology-logo" src="./docs/logo.png" width="200">
</p>

<h2 align="center">
  SolTechnology.Core
</h2>

<p align="center">
 <a>Modern readable coding</a>
</p>

<p align="center">
 <a href="https://www.nuget.org/packages?q=SolTechnology"><img src="https://img.shields.io/badge/Version-v0.2-blue?logo=nuget"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core/actions"><img src="https://github.com/AdrianStrugala/SolTechnology.Core/actions/workflows/publishPackages.yml/badge.svg"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core"><img src="https://badgen.net/badge/%E2%AD%90Stars/%E2%98%855%E2%98%85/yellow"></a>
</p>

<i>
<p align="center">
"Clean code is simple and direct. Clean code reads like well-written prose. Clean code never obscures the designer's intent but rather is full of crisp abstractions and straightforward lines of control."
</p>
<p align="right">
~Grady Booch, author of *Object-Oriented Analysis and Design with Applications*
</p>
</i>

## Core Libraries

The SolTechnology.Core repository contains a set of shared libraries. This is a foundation for CQRS-driven applications using basic Azure technologies. The libraries support a modern coding approach presented in the example applications.


| Documentation                                                                                                    | Tags                                   | NuGet                                                                                                                                                   |
|------------------------------------------------------------------------------------------------------------|----------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| [SQL Database](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Sql.md)               | `Database`, `SQL`, `ORM`, `Dapper`, `EF`               | <a href="https://www.nuget.org/packages/SolTechnology.Core.Sql/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Sql?icon=nuget&color=blue"></a>           |
| [Blob Storage](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Blob.md)              | `Azure`, `Blob`, `Storage`, `no-SQL`             | <a href="https://www.nuget.org/packages/SolTechnology.Core.BlobStorage/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.BlobStorage?icon=nuget&color=blue"></a> |
| [HTTP Clients](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Clients.md)           | `HTTP`, `Client`, `REST`               | <a href="https://www.nuget.org/packages/SolTechnology.Core.ApiClient/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.ApiClient?icon=nuget&color=blue"></a>    |
| [Message Bus](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Bus.md)                | `Azure`, `Messaging`, `Async`, `Queue`          | <a href="https://www.nuget.org/packages/SolTechnology.Core.MessageBus/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.MessageBus?icon=nuget&color=blue"></a>  |
| [Guards](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Guards.md)                  | `Validation`, `Guards`, `Checks`       | <a href="https://www.nuget.org/packages/SolTechnology.Core.Guards/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Guards?icon=nuget&color=blue"></a>        |
| [Authentication](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Auth.md)            | `Auth`, `Security`, `Basic`, `API key`              | <a href="https://www.nuget.org/packages/SolTechnology.Core.Authentication/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Authentication?icon=nuget&color=blue"></a> |
| [Logging](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Log.md)                    | `Logging`, `Diagnostics`, `Tracing`    | <a href="https://www.nuget.org/packages/SolTechnology.Core.Logging/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Logging?icon=nuget&color=blue"></a>       |
| [Scheduler](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Cron.md)                 | `Scheduler`, `Cron`, `Tasks`           | <a href="https://www.nuget.org/packages/SolTechnology.Core.Scheduler/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Scheduler?icon=nuget&color=blue"></a>    |
| [CQRS](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/CQRS.md)                      | `CQRS`, `Patterns`, `Architecture`     | <a href="https://www.nuget.org/packages/SolTechnology.Core.CQRS/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.CQRS?icon=nuget&color=blue"></a>             |
| [API](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Api.md)                        | `API`, `Web`, `Controllers`            | <a href="https://www.nuget.org/packages/SolTechnology.Core.Api/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Api?icon=nuget&color=blue"></a>               |
| [Cache](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Cache.md)                    | `Cache`, `Memory`, `Performance`       | <a href="https://www.nuget.org/packages/SolTechnology.Core.Cache/"><img src="https://badgen.net/nuget/v/SolTechnology.Core.Cache?icon=nuget&color=blue"></a>           |



## Tale Code

The idea of clean and readable code has stayed with me from the very beginning of my career. As a book lover and amateur writer, this is the most natural part of programming.\
In the Tale Code approach, I am trying to summarize all the information about coding, design, automation, and configuration that I have learned over the years. The Tale Code rule is simple:

<i>
<p align="center">
"Make your code a pleasure to read, like a tale."
</p>
<p align="right">
~Adrian Strugała
</p>
</i>

The sample application is the most common case that came to my mind. It's built of a user-facing API and a background worker responsible for fetching data and feeding the SQL database. The communication between these two is asynchronous and based on messages, as shown in the picture:

![design](./docs/taleCodeArchitecture.png)

<p>
<b>Code design is the main goal</b> of Tale Code. Logical flow and code structure are described in detail, and it even follows a more human-friendly, functional-like notation:

```csharp
var context = new CalculateBestPathContext(cities!);

var result = await Chain
     .Start(context, cancellationToken)
     .Then(_downloadRoadData)
     .Then(_findProfitablePath)
     .Then(_solveTSP)
     .End(_formResult);
```


I have summarized the knowledge and decisions into three chapters.\
Enjoy your reading! 
</p>


[1. The Design](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/theDesign.md) \
[2. The Automation](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/theAutomation.md) \
[3. The Quality](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/theQuality.md)

*Some ending words*



