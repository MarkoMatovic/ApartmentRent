using System;
using System.Collections.Generic;
namespace Lander.src.Modules.Listings.Models;
public partial class ApartmentImage
{
    public int ImageId { get; set; }
    public int? ApartmentId { get; set; }
    /// <summary>Legacy absolute URL. Retained for rows created before the blob-path migration.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Container-relative storage key for the full-size image (e.g. "2026/06/{guid}.webp").</summary>
    public string? BlobPath { get; set; }
    /// <summary>Container-relative storage key for the thumbnail (e.g. "2026/06/{guid}-thumb.webp").</summary>
    public string? ThumbnailPath { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual Apartment? Apartment { get; set; }
}
