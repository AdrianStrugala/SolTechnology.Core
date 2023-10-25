# Dream Travel

*"Observing the past centuries we can conclude, that the humanity tend to search for optimal roads. Merchants, travelers, and explorers were creating more and more detailed maps. In the digital era, everyone can use free and open real-time maps found in the Internet. Some of them provide a feature for finding the shortest road connecting a set of cities, but very few can answer common question “Is it really worth to pay for toll highway or should I just stay on the free road?” A solution of Traveling Eco-Salesman Problem provides an answer to this question.
<br>
 Program is written in C#, implements Ant Colony Optimization and God alorithms."*


 That's the original Readme of Dream Travel app, written in ~2016 as part of my master thesis. Feeling old now.
 
 ### Purpose

 The Dream Trvel app contains two parts: frontend page where user can set up a set of cities. And backend which is sorting those cities according to custom formula binding time and cost of the travel (Traveling Salesman Problem solver).

 ### Design

 For couple of years DT was my pet project, where I was testing out new patterns and code designs. That's why I am going to be quite detailed in this section.

 Projects dependency diagram:


 ```mermaid
classDiagram
    DreamTravel <|-- Api : Presentation
    Api <|-- Trips : Logic 
    Api <|-- Identity : Logic

    Trips <|-- TripsCommands  : CQRS 
    Trips <|-- TripsQueries : in

    TripsQueries <|-- TripsHttpClients : Data 

    Identity <|-- IdentityCommands : action

    IdentityCommands <|-- IdentityDatabaseClient : Data 

    IdentityDatabaseClient <|-- Infrastructure : The base 
    TripsHttpClients <|-- Infrastructure : The base 
 

    class DreamTravel{
       The complete backend 
       app design
    }

    class Api{
      The .NET host for the app
      Exposes APIs
    }

    class Trips{
      Logic layer
      Module responsible for
      the path finding
    }

    class Identity{
      Logic layer 
      Module responsible for
      user management
    }

    class Infrastructure{
      The very technical, drivers part
    }

      class TripsHttpClients{
      Data Layer
      Provides clients for HTTP calls
    }

     class IdentityDatabaseClient{
      Data Layer
      Provides SQL queries and repositories
    }
```