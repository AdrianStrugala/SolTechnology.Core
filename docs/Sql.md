### Overview

The SolTechnology.Core.SQL library provides minimum functionality needed for SQL db connection. It handles needed services registration and configuration and a result provides IDbConnection.

### Registration

For installing the library, reference **SolTechnology.Core.Sql** nuget package and invoke **AddSQL()** service collection extension method:

```csharp
services.AddSQL();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "Sql": {
      "ConnectionString": "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var sqlConfiguration = new SQLConfiguration
{
    ConnectionString = "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
};
services.AddSQL(sqlConfiguration);
```


### Usage

1) Inject ISQLConnectionFactory

```csharp
     public MatchRepository(ISQLConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }
```

2) Create IDbConnection

```csharp
  using (var connection = _sqlConnectionFactory.CreateConnection())
```

3) Works well with raw SQL and Dapper ORM.