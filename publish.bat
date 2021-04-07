@echo off

REM .NET framework dependent
dotnet publish -c Release -o bin/artifacts/framework-dependent
del bin\artifacts\framework-dependent\appsettings.Development.json

REM win-x86 self contained
dotnet publish -c Release -r win-x86 -p:PublishTrimmed=true -p:PublishSingleFile=true -o bin/artifacts/win-x86-self-contained
del bin\artifacts\win-x86-self-contained\appsettings.Development.json

REM win-x64 self contained
dotnet publish -c Release -r win-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o bin/artifacts/win-x64-self-contained
del bin\artifacts\win-x64-self-contained\appsettings.Development.json

REM linux-x64 self contained
dotnet publish -c Release -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o bin/artifacts/linux-x64-self-contained
del bin\artifacts\linux-x64-self-contained\appsettings.Development.json

REM osx-x64
dotnet publish -c Release -r osx-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o bin/artifacts/osx-x64-self-contained
del bin\artifacts\osx-x64-self-contained\appsettings.Development.json

REM osx.11.0-arm64
REM dotnet publish -c Release -r osx.11.0-arm64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o bin/artifacts/osx.11.0-arm64-self-contained
REM del bin\artifacts\osx.11.0-arm64-self-contained\appsettings.Development.json
