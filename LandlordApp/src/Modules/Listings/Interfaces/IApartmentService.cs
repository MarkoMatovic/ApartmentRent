namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>
/// Unified apartment service interface — extends both the query and command sub-interfaces
/// so existing consumers (controllers, tests) continue to work without changes.
///
/// New consumers should prefer the narrower <see cref="IApartmentQueryService"/> or
/// <see cref="IApartmentCommandService"/> interfaces directly.
///
/// Implementation: <see cref="Lander.src.Modules.Listings.Implementation.ApartmentService"/>
/// (composed of three partial classes: .cs / .Queries.cs / .Commands.cs)
/// </summary>
public interface IApartmentService : IApartmentQueryService, IApartmentCommandService
{
    // All members inherited from the two sub-interfaces.
    // No additional members — this interface is intentionally a seam only.
}
