release: cd backend/MyProject.API && dotnet publish -c Release -o ./publish || true
web: cd backend/MyProject.API/publish && ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet MyProject.API.dll || cd ../bin/Release/net8.0 && ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet MyProject.API.dll
