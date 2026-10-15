namespace Lander.src.Modules.Roommates.Models;

public enum PairingStatus
{
    /// <summary>One person proposed, the other has not answered yet.</summary>
    Pending = 0,
    /// <summary>Both agreed: they are roommates and no longer available to anybody else.</summary>
    Accepted = 1,
    Declined = 2,
    /// <summary>The proposal was withdrawn, or became void because one of them paired up with someone else.</summary>
    Cancelled = 3,
    /// <summary>They were roommates and one of them ended it; both are available again.</summary>
    Ended = 4,
}

/// <summary>
/// Two people who agreed to be roommates (or are still agreeing). While one is <see cref="PairingStatus.Accepted"/>
/// both are "taken": hidden from roommate search and unable to pair with anyone else.
/// </summary>
public class RoommatePairing
{
    public int PairingId { get; set; }
    public int RequesterUserId { get; set; }
    public int PartnerUserId { get; set; }
    public PairingStatus Status { get; set; } = PairingStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedDate { get; set; }
    public DateTime? EndedDate { get; set; }

    public virtual ICollection<PairingApartment> Apartments { get; set; } = new List<PairingApartment>();
}

/// <summary>An apartment on the shared shortlist of an accepted pair.</summary>
public class PairingApartment
{
    public int PairingApartmentId { get; set; }
    public int PairingId { get; set; }
    public int ApartmentId { get; set; }
    public int AddedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    public virtual RoommatePairing? Pairing { get; set; }
}
