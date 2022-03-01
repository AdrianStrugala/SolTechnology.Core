@minLength(1)
param baseName string = 'talecode'

@minLength(1)
param apiName string = '${baseName}api'

@minLength(1)
param sqlServerName string = '${baseName}sqlserver'

@minLength(1)
param databaseName string = baseName

@minLength(1)
param environmentName string = 'prod'

@description('Describes plan\'s pricing tier and capacity. Check details at https://azure.microsoft.com/en-us/pricing/details/app-service/')
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S0'
  'S1'
  'S2'
  'S3'
  'P1'
  'P2'
  'P3'
  'P4'
])
@minLength(1)
param sqlSkuName string = 'S0'

@description('Describes plan\'s pricing tier and capacity. Check details at https://azure.microsoft.com/en-us/pricing/details/app-service/')
@allowed([
  'Basic'
  'Shared'
  'Free'
  'Standard'
  'Premium'
  'PremiumV2'
  'Isolated'
  'Dynamic'
])
@minLength(1)
param sqlSkuTier string = 'Standard'

param apiSkuName string = 'S1'


//API-----------------------------------------------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: '${baseName}plan'
  location: resourceGroup().location
  properties: {
    reserved: true
  }
  sku: {
    name: apiSkuName
  }
  kind: 'linux'
}


resource api 'Microsoft.Web/sites@2015-08-01' = {
  name: apiName
  location: resourceGroup().location
  tags: {
    'hidden-related:${resourceGroup().id}/providers/Microsoft.Web/serverfarms/${apiName}': 'Resource'
    displayName: 'Tale Code'
  }
  properties: {
    name: apiName
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: app_insights.properties.InstrumentationKey
        }
      ]
      http20Enabled: true
      minTlsVersion: '1.2'
      autoHealEnabled: true
      autoHealRules: {
        actions: {
          actionType: 'Recycle'
        }
        triggers: {
          statusCodes: [
            {
              status: 500
            }
            {
              status: 502
            }
            {
              status: 503
            }
          ]
        }
      }
    }
  }
}




resource appsettings 'Microsoft.Web/sites/config@2015-08-01' = {
  parent: api
  location: resourceGroup().location
  name: 'appsettings'
  tags: {
    displayName: 'appsettings'
  }
  properties: {
    ASPNETCORE_ENVIRONMENT: environmentName
    Configuration__Sql__ConnectionString: 'Server=tcp:talecodesqlserver.database.windows.net,1433;Initial Catalog=talecode;Persist Security Info=False;User ID=adrian;Password=password_xxddd_2137;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}



//SQL-----------------------------------------------------------------------------------------------------------------


resource sqlserver 'Microsoft.Sql/servers@2021-08-01-preview' = {
  name: sqlServerName
  location: resourceGroup().location
  identity: {
    type: 'None'
    userAssignedIdentities: {}
  }
  properties: {
    administratorLogin: 'adrian'
    administratorLoginPassword: 'password_xxddd_2137'
    minimalTlsVersion: '1.2'
  }
}

resource database 'Microsoft.Sql/servers/databases@2017-10-01-preview' = {
  name: '${sqlServerName}/${databaseName}'
  location: resourceGroup().location
  tags: {
    displayName: 'Tale Code Database'
  }
  sku: {
    name: sqlSkuName
    tier: sqlSkuTier
  }
  properties: {}
  dependsOn: []
}

resource database_retention 'Microsoft.Sql/servers/databases/backupLongTermRetentionPolicies@2017-03-01-preview' = {
  parent: database
  name: 'default'
  properties: {
    weeklyRetention: 'P4W'
  }
}

resource app_insights 'Microsoft.Insights/components@2014-04-01' = {
  name: baseName
  location: resourceGroup().location
  tags: {
    'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/${baseName}': 'Resource'
    displayName: 'AppInsights'
  }
  properties: {
    applicationId: baseName
  }
  dependsOn: []
}
