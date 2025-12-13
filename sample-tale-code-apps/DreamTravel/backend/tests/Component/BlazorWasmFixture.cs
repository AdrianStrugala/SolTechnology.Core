using System.Diagnostics;

namespace DreamTravel.FunctionalTests;

/// <summary>
/// Fixture for running Blazor WebAssembly application during tests.
/// Starts the UI dev server and manages its lifecycle.
/// </summary>
public class BlazorWasmFixture : IAsyncDisposable
{
    private Process? _process;
    private readonly string _projectPath;
    private readonly string _baseUrl;
    private readonly int _port;

    public string BaseUrl => _baseUrl;

    public BlazorWasmFixture(string projectPath, int port = 7024)
    {
        _projectPath = projectPath;
        _port = port;
        _baseUrl = $"https://localhost:{port}";
    }

    public async Task StartAsync()
    {
        // Start dotnet run for the Blazor WASM project
        // Note: Builds the project if needed (no --no-build flag)
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            WorkingDirectory = _projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment =
            {
                ["ASPNETCORE_URLS"] = _baseUrl,
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            }
        };

        _process = Process.Start(startInfo);

        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start Blazor WASM dev server");
        }

        // Wait for the application to be ready
        await WaitForApplicationStartupAsync();
    }

    private async Task WaitForApplicationStartupAsync()
    {
        using var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Accept self-signed cert
        });

        var maxAttempts = 60; // 60 seconds timeout
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            try
            {
                var response = await httpClient.GetAsync(_baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Give it a bit more time to fully initialize
                    await Task.Delay(2000);
                    return;
                }
            }
            catch
            {
                // Application not ready yet
            }

            await Task.Delay(1000);
            attempt++;
        }

        throw new TimeoutException($"Blazor WASM application did not start within {maxAttempts} seconds at {_baseUrl}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            try
            {
                // Try graceful shutdown first
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch
            {
                // Process might have already exited
            }

            _process.Dispose();
            _process = null;
        }
    }
}
