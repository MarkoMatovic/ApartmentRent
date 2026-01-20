using System;
using System.Collections.Generic;
namespace Lander.src.Modules.Listings.Models;
public partial class ApartmentImage
{
    public int ImageId { get; set; }
    public int? ApartmentId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual Apartment? Apartment { get; set; }
}
