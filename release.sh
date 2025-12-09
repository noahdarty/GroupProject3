#!/bin/bash
cd backend/MyProject.API
dotnet publish -c Release -r linux-x64 --self-contained true -o publish

