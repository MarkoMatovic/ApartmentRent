namespace LandlordApp.Tests.E2eTests.Infrastructure;

/// <summary>
/// Declares the xUnit collection that all E2E test classes join via [Collection(E2eCollection.Name)].
/// One <see cref="E2eFixture"/> instance is shared across the entire collection:
/// the SQL Server container starts once, migrations run once, and Respawn resets state before each test.
/// </summary>
[CollectionDefinition(Name)]
public class E2eCollection : ICollectionFixture<E2eFixture>
{
    public const string Name = "e2e";
}
