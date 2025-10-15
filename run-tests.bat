@echo off
echo Running FileSharer Tests
echo ========================

echo.
echo Running Backend Tests...
echo ------------------------
dotnet test FileSharer.Tests/FileSharer.Tests.csproj --verbosity normal

echo.
echo Running Frontend Tests...
echo ------------------------
cd file-sharing-client
npm test -- --coverage --watchAll=false
cd ..

echo.
echo All tests completed!
pause
