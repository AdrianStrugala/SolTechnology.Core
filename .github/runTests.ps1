dotnet vstest (Get-ChildItem tests | % { Join-Path $_.FullName -ChildPath ("bin/Release/net6.0/$($_.Name).dll") })