using Lander.src.Modules.Roommates.Dtos.Dto;

namespace Lander.src.Modules.Roommates.Interfaces;

public interface IRoommatePairingService
{
    Task<MyPairingStateDto> GetMyStateAsync(int userId);

    /// <summary>Proposes to be roommates. If the other person already proposed to this user, that proposal is accepted instead.</summary>
    Task<PairingDto> ProposeAsync(int userId, int partnerUserId);
    Task<PairingDto> AcceptAsync(int userId, int pairingId);
    Task DeclineAsync(int userId, int pairingId);
    Task CancelAsync(int userId, int pairingId);
    /// <summary>Either roommate ends an accepted pairing; both become available again.</summary>
    Task EndAsync(int userId, int pairingId);

    /// <summary>Id of the user's roommate, or null when the user has none.</summary>
    Task<int?> GetActivePartnerIdAsync(int userId);
    Task<bool> AreRoommatesAsync(int userId, int otherUserId);

    /// <summary>
    /// True when <paramref name="landlordUserId"/> rents out an apartment on the shared list of the user's pairing.
    /// That is reason enough for the pair to talk to them together, with no earlier conversation needed.
    /// </summary>
    Task<bool> IsLandlordOfSharedApartmentAsync(int userId, int landlordUserId);

    /// <summary>Closes every open or active pairing of a user who is being deleted.</summary>
    Task EndAllForUserAsync(int userId);

    Task<List<SharedApartmentDto>> GetSharedApartmentsAsync(int userId);
    Task<SharedApartmentDto> ShareApartmentAsync(int userId, int apartmentId, string? note);
    Task<bool> RemoveSharedApartmentAsync(int userId, int apartmentId);
}
