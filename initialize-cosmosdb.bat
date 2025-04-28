@echo off
echo Building and running Cosmos DB Initializer...
echo.

cd %~dp0CosmosInitializer
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Failed to build Cosmos DB Initializer.
    exit /b %ERRORLEVEL%
)

echo.
dotnet run
if %ERRORLEVEL% NEQ 0 (
    echo Failed to initialize Cosmos DB.
    exit /b %ERRORLEVEL%
)

echo.
echo Cosmos DB initialization completed.
cd %~dp0