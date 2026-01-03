To give you the time to focus on the code, there is one important piece that has to be done right. And this is the infrastructure of your service.
The rules that I am following are:
1. Do your configuration only once
2. Automate everything
3. Hide the secrets
4. Have your pipeline always green:
<img alt="design" src="./pipeline.PNG">

## Infrastructure as Code

It's convenient to have the service infrastructure written as code. That enables all of the visibility and tracking of GitHub and ensures the repeatability and reliability of the deployments.

In the DreamTravel app case, Bicep (ARM Template overlay) is used: [main.bicep](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/sample-tale-code-apps/DreamTravel/devOps/infrastructure/main.bicep)

It contains the following sections:
* **appServicePlan** (App Service Plan - Linux)
  - API (DreamTravel.Api)
  - Worker (DreamTravel.Worker - background processing)
  - appSettings for each
* **SQL Server**
  - SQL Database (DreamTravelDatabase)
* **Application Insights** (monitoring and logging)
* **Storage Account** (blob storage)
* **Service Bus** (message broker)

That's actually all that is needed to run the full app on Azure!


## Automated CI/CD

The same reasoning stays behind pipelines as code. Handles testing, building, and deployment of the sample application: [build&test.yml](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/sample-tale-code-apps/DreamTravel/devOps/pipelines/build&test.yml)

**Stage 1: Test**
- Run SQL Server in local Docker container
- Deploy Database to local server using DreamTravelDatabase project
- Run Unit Tests (`**/*UnitTests.csproj`)
- Run Integration Tests (`**/*IntegrationTests.csproj`)
- Run Component Tests (`**/*Component.Tests.csproj`)

**Stage 2: Build & Publish** (only on non-PR builds)
- Publish Infrastructure templates (Bicep files)
- Publish Database project (dacpac)
- Publish DreamTravel.Api
- Publish DreamTravel.Worker
- Upload artifacts for deployment

**Stage 3: Deploy** (separate pipeline: [deploy.yml](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/sample-tale-code-apps/DreamTravel/devOps/pipelines/deploy.yml))
- Apply environment-specific settings
- Deploy Infrastructure (from Bicep file)
- Deploy Database
- Deploy Apps

**E2E Tests** (separate pipeline: [e2etests.yml](https://github.com/AdrianStrugala/SolTechnology.Core/blob/master/sample-tale-code-apps/DreamTravel/devOps/pipelines/e2etests.yml))
- Can be triggered independently
- Runs `**/*E2E.Tests.csproj`

**Benefits of this configuration:**
1) **The app is tested by unit, integration, and component tests**
2) **The database changes are tested**
3) **It's not dependent on external resources (what is needed is run locally)**



## Manage Secrets

There is a cool feature built into Azure DevOps that makes secrets management a pleasure: **Variable Groups** in Library.

<img alt="design" src="./library.PNG">

### Azure DevOps Variable Groups

Using it is simple. By adding:
```yaml
variables:
 - group: dream-travel-test
```

To `pipeline.yml` file, the variables can now be referenced as:
```yaml
$(sqlAdminUserName)
$(sqlAdminPassword)
```

**Example from build&test.yml:**
```yaml
stages:
  - stage: Test
    displayName: Test
    variables:
       - group: dream-travel-test  # Loads secrets for test stage
    jobs:
      - job: RunTests
        steps:
          # Variables like connection strings are now available
```

What's even more cool, by applying JSON format transformation, they can be applied directly to appsettings:
```yaml
- task: FileTransform@1
    displayName: 'Apply Production App settings for Api'
    inputs:
      folderPath: '$(Build.ArtifactStagingDirectory)/**/*Api.zip'
      fileType: 'json'
      targetFiles: '**/appsettings.json'
```
