### Overview

The SolTechnology.Core.Authentication library provides minimum basic API Key Authentication functionality. It handles needed services registration and configuration.

### Registration

For installing the library, reference **SolTechnology.Core.Authentication** nuget package and invoke **AddAuthenticationAndBuildFilter();** service collection extension method:

```csharp
var authenticationFiler = builder.Services.AddAuthenticationAndBuildFilter();
```

### Configuration

1) The first option is to create an appsettings.json section:

```csharp
  "Configuration": {
    "Authentication": {
      "Key": "SecureKey"
    }
  }
```

2) Alternatevely the same settings can be provided by optional parameter during registration:

```csharp
var authenticationConfiguration = new AuthenticationConfiguration
{
    Key = "SecureKey"
};
services.AddAuthenticationAndBuildFilter(authenticationConfiguration);
```


### Usage

1) After registration, the authentication filter has to be added to MVC options

```csharp
builder.Services.AddControllers(opts => opts.Filters.Add(authenticationFiler));
```

2) Additionally Authentication and Authorization has to be added to app

```csharp
app.UseAuthorization();
app.UseAuthentication();
```

3) From now on, you have to add your authentication key to header of every request send to the application

- Header name: "X-Auth"
- Schema name: "SolTechnologyAuthentication"
- Base64 encoded key: (equivalent of "SecureKey" is "U2VjdXJlS2V5") 

For example:
```csharp
"X-Auth": "SolTechnologyAuthentication U2VjdXJlS2V5"
```
