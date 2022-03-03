
//BASE PARAMS
param baseName string = 'talecode'
param environmentName string = 'prod'
param location string = resourceGroup().location
param testParam string


//API PARAMS
param appServicePlanName string = '${baseName}plan'
param apiName string
param apiSkuName string = 'F1'


//SQL PARAMS
param sqlServerName string
param databaseName string
param sqlAdminUserName string
param sqlAdminPassword string

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




//API-----------------------------------------------------------------------------------------------------------------

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  properties: {
    reserved: true
  }
  sku: {
    name: apiSkuName
  }
  kind: 'linux'
}


resource api 'Microsoft.Web/sites@2021-02-01' = {
  name: apiName
  location: location
  tags: {
    'hidden-related:${resourceGroup().id}/providers/Microsoft.Web/serverfarms/${apiName}': 'Resource'
    displayName: 'Tale Code'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      ftpsState: 'Disabled'
      netFrameworkVersion: 'v6.0'
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
  location: location
  name: 'appsettings'
  tags: {
    displayName: 'appsettings'
  }
  properties: {
    ASPNETCORE_ENVIRONMENT: environmentName
    TEST_PARAM: testParam
    APPINSIGHTS_INSTRUMENTATIONKEY: app_insights.properties.InstrumentationKey
  }
}



//SQL-----------------------------------------------------------------------------------------------------------------


resource sqlserver 'Microsoft.Sql/servers@2021-08-01-preview' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'None'
    userAssignedIdentities: {}
  }
  properties: {
    administratorLogin: sqlAdminUserName
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource database 'Microsoft.Sql/servers/databases@2017-10-01-preview' = {
  name: '${sqlServerName}/${databaseName}'
  location: location
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
  location: location
  tags: {
    'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/${baseName}': 'Resource'
    displayName: 'AppInsights'
  }
  properties: {
    applicationId: baseName
  }
  dependsOn: []
}
