using TicketFlow.Shared.Architecture;
using Xunit;

namespace TicketFlow.Services.Inquiries.FitnessTests;

public class FitnessTests
{
    private static readonly string SourcePath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string AppsettingsPath = Path.Combine(
        SourcePath, "TicketFlow.Services.Inquiries.Api", "appsettings.json");

    [Fact]
    public void AppName_Should_Match_ServiceName()
        => FitnessFunctions.ValidateAppName(AppsettingsPath, "inquiries-service");

    [Fact]
    public void Should_Not_Have_Hardcoded_ConnectionStrings()
        => FitnessFunctions.ValidateNoHardcodedConnectionStrings(SourcePath);

    [Fact]
    public void Should_Use_Own_Database()
        => FitnessFunctions.ValidateOwnDatabase(AppsettingsPath, "TicketFlow.Inquiries");
    
    [Fact]
    public void Should_Use_HttpClientFactory_Instead_Of_Direct_Instantiation()
        => FitnessFunctions.ValidateNoDirectHttpClientInstantiation(SourcePath);

    [Fact]
    public void Should_Not_Use_SyncOverAsync()
        => FitnessFunctions.ValidateNoSyncOverAsync(SourcePath);
}
