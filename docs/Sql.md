

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