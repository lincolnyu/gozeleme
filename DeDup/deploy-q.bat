dotnet build . --configuration Release --runtime win-x86
xcopy bin\Release\netcoreapp3.1\win-x86 q:\.netools\dedup /s /y /i
