Framework "4.6"

properties{
    $basePath = $PSScriptRoot
    $srcPath = (Get-Item "$basePath").Parent.FullName
	$binPath = "$srcPath\bin"
	$testsPath = "$srcPath\tests"
	#configuration of database preparations
	$databaseServer = "localhost,1433"
	# put here if you want to build any additional database
	
	$projectPath = "$srcPath\src\Backend\DreamTravelDatabase\DreamTravelDatabase.csproj"
	$profilePathRelativeToOutput = "_Deployment\default.publish.xml"
	$projectName = [io.path]::GetFileNameWithoutExtension($projectPath)
	$databaseName = "$projectName"
}

task Start-SQLServer{
	Write-Host "Starting SQL Server in the Docker"

	$dockerContainerName = "Test-SqlServer"

	if((docker container ls -a -f name=$dockerContainerName).Count -le 1){
		Write-Host "Running SQL Server container"
		docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=password_xxddd_2137' -p 1433:1433 --name $dockerContainerName -d mcr.microsoft.com/mssql/server:2019-latest
	}
	else{
		if((docker container ls -f name=$dockerContainerName).Count -le 1){
			Write-Host "Starting SQL Server container"
			docker start $dockerContainerName
		}
		else{
			Write-Host "SQL Server is already running"
		}
	}


}

task Create-Database{
	Write-Host "Publishing Database to the SQL Server"

	Write-Host $databaseName
	$dacpacPath = "$binPath\$projectName"

	dotnet publish $projectPath /p:TargetServerName=localhost /p:TargetPort=1433 /p:TargetUser=sa /p:TargetPassword=password_xxddd_2137 /p:TargetDatabaseName=$databaseName

	Write-Host "Database [$databaseName] successfully published"

}

Task Invoke-FullChain -Depends Start-SQLServer,Create-Database
