

<p align="center">
    <img alt="SolTechnology-logo" src="./docs/logo.png" width="200">
</p>

<h2 align="center">
  SolTechnology.Core
</h2>

<p align="center">
 <a> Modern redable coding </a>
</p>

<p align="center">
 <a href="https://www.nuget.org/packages?q=SolTechnology"><img src="https://img.shields.io/badge/Version-v0.2-blue?logo=nuget"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core/actions"><img src="https://github.com/AdrianStrugala/SolTechnology.Core/actions/workflows/publishPackages.yml/badge.svg"></a>
 <a href="https://github.com/AdrianStrugala/SolTechnology.Core"><img src="https://badgen.net/badge/%E2%AD%90Stars/%E2%98%852%E2%98%85/yellow"></a>

</p>


<i>
<p align="center">
"Clean code is simple and direct. Clean code reads like well-written prose. Clean code never obscures the designer's intent but rather is full of crisp abstractions and straightforward lines of control."
</p>
<p align="right">
*~Grady Booch author of Object Oriented Analysis and Design with Applications*
</p>
</i>


## Core Libraries


The SolTechnology.Core repository contains a set of shared libraries. This is a foundation for CQRS driven applications using basic Azure technologies. The libraries support a modern coding approach presented in the example application called "Tale Code".


| Documentation  | Nuget  |   |
|---|---|---|
|[Sql Database](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Sql.md)      |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Sql/"><img src="https://badgen.net/badge/Downloads/150/?icon=nuget"></a>  |   |
|[Blob Storage](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Blob.md)     |  <a href="https://www.nuget.org/packages/SolTechnology.Core.BlobStorage/"><img src="https://badgen.net/badge/Downloads/100/?icon=nuget"></a>  |   |
|[HttpClients](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Clients.md)   |  <a href="https://www.nuget.org/packages/SolTechnology.Core.ApiClient/"><img src="https://badgen.net/badge/Downloads/150/?icon=nuget"></a>  |   |
|[Message Bus](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Bus.md)       |  <a href="https://www.nuget.org/packages/SolTechnology.Core.MessageBus/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Guards](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Guards.md)         |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Guards/"><img src="https://badgen.net/badge/Downloads/100/?icon=nuget"></a>  |   |
|[Authentication](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Auth.md)   |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Authentication/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Logging](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Log.md)           |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Logging/"><img src="https://badgen.net/badge/Downloads/50/?icon=nuget"></a>  |   |
|[Scheduler](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/docs/Cron.md)        |  <a href="https://www.nuget.org/packages/SolTechnology.Core.Scheduler/"><img src="https://badgen.net/badge/Downloads/0/?icon=nuget"></a>  |   |



## Tale Code




The idea of clean and readable code stays with me from the very beginning of my career. As for book lover and amateur writer, this is the most natural part of programming.\
In the Tale Code approach, I am trying to summarize all the information about coding, design, automation and configuration that I have learned over the years.
The Tale Code rule is simple:

<p align="center">
<b> "Make your code pleasure to read like a tale."
</b>
</p>

The TaleCode application is the most common case that came to my mind. Every night *some* data is fetched. Then it is validated and stored. Users have the possibility to query the data. The queries are expensive and require additional data manipulation.\
To solve the application flow the CQRS approach is implemented. It is interesting from a technological perspective. Uses SQL, no-SQL databases, Azure Service Bus, Scheduled Tasks, Authentication, and Application Insights logging.
I have summarized the knowledge and decisions into three chapters. \
Enjoy your reading! 



[1) The Design](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/taleCode/docs/theDesign.md) \
[2) The Automatization](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/taleCode/docs/theAutomatization.md) \
[2) The Quality](https://github.com/AdrianStrugala/SolTechnology.Core/tree/master/taleCode/docs/theQuality.md)

*Some ending words*



