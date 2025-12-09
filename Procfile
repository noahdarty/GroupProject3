release: cd backend/MyProject.API && dotnet publish -c Release -o bin/Release/net8.0/publish
web: cd backend/MyProject.API/bin/Release/net8.0/publish && ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet MyProject.API.dll
