using System.Reflection;
using SolTechnology.Core.Testing.Containers;
using Testcontainers.ServiceBus;

namespace SolTechnology.Core.ServiceBus.Testing;

/// <summary>
/// Builds the Azure Service Bus emulator container.
/// </summary>
/// <remarks>
/// <para>
/// The emulator persists its state in SQL Server. Without <c>WithMsSqlContainer</c>, <c>Build()</c> creates
/// an MSSQL sidecar and a network that disposing the emulator container does not remove, so
/// <see cref="ServiceBusFixture"/> wires its own through <c>WithMsSqlContainer</c> and disposes them. Attaching
/// any other network or <c>DependsOn</c> makes the emulator's <c>UnsafeCreateAsync</c> throw
/// "Sequence contains more than one element". The fixture does not share a SQL container with
/// <c>SolTechnology.Core.SQL.Testing</c>: the 4.x emulator API requires a concrete <c>MsSqlContainer</c>,
/// incompatible with SQL.Testing's generic-builder engine.
/// </para>
/// </remarks>
public static class ServiceBusInstanceBuilder
{
    private const string DefaultImage = "mcr.microsoft.com/azure-messaging/servicebus-emulator:latest";
    private const string ContainerConfigDir = "/ServiceBus_Emulator/ConfigFiles/";
    private const string ConfigFileName = "Config.json";
    private const string EmbeddedConfigName = "SolTechnology.Core.ServiceBus.Testing.servicebus-emulator-config.json";

    /// <summary>
    /// Creates a <see cref="ServiceBusBuilder"/>. Pass <paramref name="configFilePath"/> to use a custom
    /// emulator topology; when null the bundled default config (one queue + one topic/subscription on
    /// namespace <c>sbemulatorns</c>) is used. The file is normalised to a temp <c>Config.json</c> and
    /// copied into the emulator's config directory.
    /// </summary>
    public static ServiceBusBuilder CreateBuilder(string? configFilePath = null, string? image = null)
    {
        var configPath = PrepareConfig(configFilePath);

        var builder = new ServiceBusBuilder(image ?? DefaultImage)
            .WithAcceptLicenseAgreement(true)
            // Target is the directory; Testcontainers writes the source file (Config.json) into it.
            .WithResourceMapping(configPath, ContainerConfigDir);

        // ServiceBusFixture reuses by name (the reuse hash is unstable with a sidecar), so keep the container instead of WithReuse.
        builder = TestContainersContext.ReuseContainers
            ? builder.WithCleanUp(false).WithAutoRemove(false)
            : builder.WithCleanUp(true).WithAutoRemove(true);

        return builder;
    }

    /// <summary>
    /// Copies the chosen config (bundled default or the caller's file) into a fresh temp directory as
    /// <c>Config.json</c> and returns its path. Normalising the name is required because
    /// <c>WithResourceMapping</c> appends the source filename to the target directory, and the emulator
    /// expects exactly <c>Config.json</c>.
    /// </summary>
    private static string PrepareConfig(string? configFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sb-emulator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var target = Path.Combine(tempDir, ConfigFileName);

        if (configFilePath is null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(EmbeddedConfigName)
                ?? throw new InvalidOperationException($"Embedded config '{EmbeddedConfigName}' not found.");
            using var file = File.Create(target);
            stream.CopyTo(file);
        }
        else
        {
            File.Copy(Path.GetFullPath(configFilePath), target, overwrite: true);
        }

        return target;
    }
}

