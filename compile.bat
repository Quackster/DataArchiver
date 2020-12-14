dotnet publish --self-contained -c Release -r ubuntu.16.10-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
dotnet publish --self-contained -c Release -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
pause