web: cd backend/MyProject.API && find . -name "MyProject.API.dll" -type f 2>/dev/null | head -1 | xargs -I {} sh -c 'cd $(dirname {}) && ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet MyProject.API.dll'
