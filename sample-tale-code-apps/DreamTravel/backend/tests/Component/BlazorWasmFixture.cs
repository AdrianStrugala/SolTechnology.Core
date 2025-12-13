using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Blazor WASM fixture - similar to ApiFixture but for static WASM apps.
/// Uses Kestrel with real port (not TestServer) because Playwright needs real URLs.
///
/// Performance optimizations:
/// 1. Publishes WASM once (smart caching - only if source changed)
/// 2. Serves via lightweight Kestrel server (~2s startup vs ~15s for 'dotnet run')
/// 3. HTTP instead of HTTPS (no certificate overhead)
/// </summary>
public class BlazorWasmFixture : IAsyncDisposable
{
    private IHost? _host;
    private readonly string _publishPath;
    private readonly string _projectPath;
    private readonly int _port;
    private readonly string? _apiBaseUrlOverride;

    public string BaseUrl { get; private set; }

    public BlazorWasmFixture(string projectPath, int port = 7024, string? apiBaseUrlOverride = null)
    {
        _projectPath = projectPath;
        _port = port;
        _publishPath = Path.Combine(_projectPath, "bin", "Debug", "net10.0", "publish", "wwwroot");
        _apiBaseUrlOverride = apiBaseUrlOverride;
        BaseUrl = $"http://localhost:{port}"; // HTTP for faster startup (no cert issues)
    }

    public async Task StartAsync()
    {
        // Step 1: Publish Blazor WASM (only if outdated)
        await PublishBlazorWasmAsync();

        // Step 1.5: Override appsettings.json for test environment
        if (_apiBaseUrlOverride != null)
        {
            OverrideAppSettings();
        }

        // Step 2: Start lightweight Kestrel server for static files
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseKestrel(options =>
                    {
                        options.ListenLocalhost(_port); // Real HTTP server with port
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddRouting(); // Required for UseRouting/UseEndpoints
                    })
                    .Configure(app =>
                    {
                        var fileProvider = new PhysicalFileProvider(_publishPath);

                        app.UseDefaultFiles(new DefaultFilesOptions
                        {
                            FileProvider = fileProvider
                        });

                        app.UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = fileProvider,
                            ServeUnknownFileTypes = true, // .wasm, .dat, .json
                            OnPrepareResponse = ctx =>
                            {
                                // CORS headers for WASM
                                ctx.Context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                                ctx.Context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                            }
                        });

                        // SPA fallback routing - serve index.html for all non-file requests
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapFallbackToFile("index.html", new StaticFileOptions
                            {
                                FileProvider = fileProvider
                            });
                        });
                    });
            });

        _host = await builder.StartAsync();

        // Wait for server readiness (Kestrel is usually instant, but be safe)
        await Task.Delay(500);
    }

    private async Task PublishBlazorWasmAsync()
    {
        // Smart caching: only publish if source files changed
        var indexPath = Path.Combine(_publishPath, "index.html");
        if (File.Exists(indexPath))
        {
            var publishTime = File.GetLastWriteTimeUtc(indexPath);
            var csprojPath = Directory.GetFiles(_projectPath, "*.csproj").FirstOrDefault();

            if (csprojPath != null)
            {
                var csprojTime = File.GetLastWriteTimeUtc(csprojPath);
                var pagesTime = Directory.Exists(Path.Combine(_projectPath, "Pages"))
                    ? Directory.GetLastWriteTimeUtc(Path.Combine(_projectPath, "Pages"))
                    : DateTime.MinValue;
                var componentsTime = Directory.Exists(Path.Combine(_projectPath, "Components"))
                    ? Directory.GetLastWriteTimeUtc(Path.Combine(_projectPath, "Components"))
                    : DateTime.MinValue;

                var latestSourceChange = new[] { csprojTime, pagesTime, componentsTime }.Max();

                if (publishTime > latestSourceChange)
                {
                    return; // Cache hit - already published and up-to-date
                }
            }
        }

        // Publish WASM
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "publish -c Debug --nologo",
            WorkingDirectory = _projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var publishProcess = System.Diagnostics.Process.Start(startInfo);
        if (publishProcess == null)
        {
            throw new InvalidOperationException("Failed to start dotnet publish");
        }

        // CRITICAL: Read output asynchronously to prevent deadlock
        // If we don't consume stdout/stderr, buffers fill up and process hangs
        var outputTask = publishProcess.StandardOutput.ReadToEndAsync();
        var errorTask = publishProcess.StandardError.ReadToEndAsync();

        await publishProcess.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (publishProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"Blazor WASM publish failed:\n{error}\n{output}");
        }
    }

    private void OverrideAppSettings()
    {
        // Override appsettings.json in publish folder to point to WireMock
        var appSettingsPath = Path.Combine(_publishPath, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            throw new InvalidOperationException($"appsettings.json not found at {appSettingsPath}");
        }

        var json = File.ReadAllText(appSettingsPath);
        var jsonNode = JsonNode.Parse(json);

        if (jsonNode == null)
        {
            throw new InvalidOperationException("Failed to parse appsettings.json");
        }

        // Override ApiBaseUrl to point to WireMock
        jsonNode["ApiBaseUrl"] = _apiBaseUrlOverride!;

        var options = new JsonSerializerOptions { WriteIndented = true };
        var updatedJson = jsonNode.ToJsonString(options);
        File.WriteAllText(appSettingsPath, updatedJson);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
