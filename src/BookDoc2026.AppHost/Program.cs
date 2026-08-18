var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.BookDoc2026_Api>("api");

builder.AddProject<Projects.BookDoc2026_Admin>("admin")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

builder.AddProject<Projects.BookDoc2026_Portal>("portal")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

var mobileProjectDirectory = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "BookDoc2026.Mobile"));

builder.AddExecutable(
        "mobile-windows",
        "dotnet",
        mobileProjectDirectory,
        "build",
        "BookDoc2026.Mobile.csproj",
        "-t:Run",
        "-f",
        "net10.0-windows10.0.19041.0")
    .WithEnvironment("Api__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api)
    .WithExplicitStart();

builder.Build().Run();
