using Lander.src.Modules.Roommates.Models;

namespace Lander.src.Modules.Roommates.Dtos.Dto;

/// <summary>One pairing seen from the point of view of the signed-in user.</summary>
public class PairingDto
{
    public int PairingId { get; set; }
    public PairingStatus Status { get; set; }
    public int PartnerUserId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerProfilePicture { get; set; }
    /// <summary>True when the other person proposed it and the signed-in user has to answer.</summary>
    public bool IsIncoming { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? RespondedDate { get; set; }
}

/// <summary>Everything the "my roommate" page needs in one call.</summary>
public class MyPairingStateDto
{
    /// <summary>The accepted pairing, if the user already has a roommate.</summary>
    public PairingDto? Active { get; set; }
    public List<PairingDto> Incoming { get; set; } = new();
    public List<PairingDto> Outgoing { get; set; } = new();
}

public class SharedApartmentDto
{
    public int ApartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public decimal Rent { get; set; }
    public decimal? Price { get; set; }
    /// <summary>Who rents it out, so the pair can open a shared conversation with them straight from the list.</summary>
    public int? LandlordUserId { get; set; }
    public string? LandlordName { get; set; }
    /// <summary>False when the listing was deleted or has expired since it was added.</summary>
    public bool IsAvailable { get; set; }
    public int AddedByUserId { get; set; }
    public string AddedByName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime AddedDate { get; set; }
}

public class ProposePairingInputDto
{
    public int PartnerUserId { get; set; }
}

public class ShareApartmentInputDto
{
    public int ApartmentId { get; set; }
    public string? Note { get; set; }
}
