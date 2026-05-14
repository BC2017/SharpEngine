# SharpEngine Tests

The initial test projects are dependency-free console test harnesses because this environment currently has .NET runtimes but no .NET SDK or restored NuGet test packages.

Once the SDK is available, run:

```powershell
dotnet run --project tests/SharpEngine.Core.Tests/SharpEngine.Core.Tests.csproj
dotnet run --project tests/SharpEngine.Rendering.Tests/SharpEngine.Rendering.Tests.csproj
dotnet run --project tests/SharpEngine.World.Tests/SharpEngine.World.Tests.csproj
```

After the SDK and package restore workflow are confirmed, these can be converted to xUnit or NUnit projects and wired into `dotnet test`.
