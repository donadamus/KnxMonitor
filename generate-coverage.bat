@echo off
echo Running tests with coverage...
dotnet test KnxTest --collect:"XPlat Code Coverage" --results-directory TestResults

echo Generating HTML coverage report...
reportgenerator -reports:"TestResults\*\coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html

echo Opening coverage report...
start CoverageReport\index.html

echo Coverage report generated successfully!