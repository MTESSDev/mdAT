REM $env:VSTEST_HOST_DEBUG=1
dotnet test --collect:"XPlat Code Coverage" --settings .\..\.runsettings
reportgenerator -reports:.\TestResults\*\coverage.cobertura.xml -targetdir:coveragereport -assemblyfilters:"-*Tests;"
del /s /q  .\TestResults\
rmdir /s /q  .\TestResults\