using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace TicketFlow.Shared.Architecture;

/// <summary>
/// Simple fitness function validators for microservices architecture.
/// </summary>
public static class FitnessFunctions
{
    /// <summary>
    /// Checks if App:AppName in appsettings.json matches expected service name.
    /// </summary>
    public static void ValidateAppName(string appsettingsPath, string expectedServiceName)
    {
        var config = LoadConfig(appsettingsPath);

        config["App:AppName"].Should().Be(expectedServiceName,
            "App name in config should match service name");
    }

    /// <summary>
    /// Checks that source code doesn't contain hardcoded connection strings.
    /// Looks for patterns like "Host=...Database=" or "Server=...Database=".
    /// </summary>
    public static void ValidateNoHardcodedConnectionStrings(string sourceCodePath)
    {
        var forbiddenPatterns = new[] { "Host=", "Server=", "Data Source=" };
        ValidateNoForbiddenStrings(sourceCodePath, forbiddenPatterns);
    }

    /// <summary>
    /// Checks that source code doesn't contain hardcoded URLs to other services.
    /// </summary>
    public static void ValidateNoHardcodedServiceUrls(string sourceCodePath, params string[] otherServiceUrls)
    {
        ValidateNoForbiddenStrings(sourceCodePath, otherServiceUrls);
    }

    /// <summary>
    /// Checks that source code uses IHttpClientFactory instead of new HttpClient().
    /// Direct HttpClient instantiation can cause socket exhaustion issues.
    /// </summary>
    public static void ValidateNoDirectHttpClientInstantiation(string sourceCodePath)
    {
        ValidateNoForbiddenStrings(sourceCodePath, "new HttpClient(");
    }

    /// <summary>
    /// Checks for sync-over-async anti-patterns.
    /// Detects .Result, .Wait(), .GetAwaiter().GetResult() usage.
    /// </summary>
    public static void ValidateNoSyncOverAsync(string sourceCodePath)
    {
        var syncOverAsyncPatterns = new[]
        {
            ".Result",              // Task.Result blocks the thread
            ".Wait()",              // Task.Wait() blocks the thread
            ".GetAwaiter().GetResult()"  // Also blocks
        };
        ValidateNoForbiddenStrings(sourceCodePath, syncOverAsyncPatterns);
    }

    /// <summary>
    /// Checks that service uses its own database, not another service's database.
    /// Validates that Database= in connection string matches expected database name.
    /// </summary>
    public static void ValidateOwnDatabase(string appsettingsPath, string expectedDatabaseName)
    {
        var config = LoadConfig(appsettingsPath);
        var connectionString = config["Postgres:ConnectionString"] ?? "";

        // Extract database name from connection string
        var dbNameStart = connectionString.IndexOf("Database=", StringComparison.OrdinalIgnoreCase);
        if (dbNameStart is -1)
        {
            throw new InvalidOperationException("No Database= found in connection string");
        }

        dbNameStart += "Database=".Length;
        var dbNameEnd = connectionString.IndexOf(';', dbNameStart);
        var actualDatabase = dbNameEnd == -1
            ? connectionString[dbNameStart..]
            : connectionString[dbNameStart..dbNameEnd];

        actualDatabase.Should().Be(expectedDatabaseName,
            $"Service should use its own database '{expectedDatabaseName}', not '{actualDatabase}'");
    }

    /// <summary>
    /// Checks that source code doesn't contain forbidden strings.
    /// </summary>
    private static void ValidateNoForbiddenStrings(string sourceCodePath, params string[] forbiddenStrings)
    {
        if (!Directory.Exists(sourceCodePath) || forbiddenStrings.Length == 0)
            return;

        var violations = new List<string>();

        var csFiles = Directory.GetFiles(sourceCodePath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/") && !f.Contains("FitnessTests"));

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            
            foreach (var forbidden in forbiddenStrings)
            {
                if (content.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{Path.GetFileName(file)}: contains '{forbidden}'");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Source code should not contain forbidden strings: {string.Join(", ", violations)}");
    }

    private static IConfiguration LoadConfig(string appsettingsPath)
    {
        var directory = Path.GetDirectoryName(appsettingsPath)!;
        var fileName = Path.GetFileName(appsettingsPath);

        return new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile(fileName)
            .Build();
    }
}
