namespace Lander.src.Modules.ApartmentApplications.Dtos.InputDto;

public class CreateApplicationInputDto
{
    public int ApartmentId { get; set; }
    public bool IsPriority { get; set; } = false;
}
