### Overview

The SolTechnology.Core.Sql library provides minimum functionality needed for SQL db connection. It handles needed services registration and configuration and a result provides IDbConnection.

### Registration

For installing the library, reference **SolTechnology.Core.Sql** nuget package and invoke **AddSql()** service collection extension method:

```csharp
services.AddSql();
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
var sqlConfiguration = new SqlConfiguration
{
    ConnectionString = "Data Source=localhost,1401;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True"
};
services.AddSql(sqlConfiguration);
```


### Usage

1) Inject ISqlConnectionFactory

```csharp
     public MatchRepository(ISqlConnectionFactory sqlConnectionFactory)
        {
            _sqlConnectionFactory = sqlConnectionFactory;
        }
```

2) Create IDbConnection

```csharp
  using (var connection = _sqlConnectionFactory.CreateConnection())
```

3) Works well with raw SQL and Dapper ORM.