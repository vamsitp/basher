# `burner`
> **A FUN (`dotnet`) TOOL TO TRACK AZURE DEVOPS TASKS/BUGS BY USER!**

![Snapshot](Snapshot.png)

> [dotnet global tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

```bash
# Publish package to nuget.org
nuget push ./bin/burner.1.0.2.nupkg -ApiKey <key> -Source https://api.nuget.org/v3/index.json

# Install from nuget.org
dotnet tool install -g burner
dotnet tool install -g burner --version 1.0.x

# Install from local project path
dotnet tool install -g --add-source ./bin burner

# Uninstall
dotnet tool uninstall -g burner
```
> NOTE: If the Tool is not accesible post installation, add `%USERPROFILE%\.dotnet\tools` to the PATH env-var.
