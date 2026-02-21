using Lander.src.Modules.ApartmentApplications.Models;

namespace Lander.src.Modules.ApartmentApplications.Interfaces;

/// <summary>
/// Service for checking application approval status
/// </summary>
public interface IApplicationApprovalService
{
    /// <summary>
    /// Checks if a user has an approved application for a specific apartment
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <param name="apartmentId">The apartment ID to check</param>
    /// <returns>True if the user has an approved application, false otherwise</returns>
    Task<bool> HasApprovedApplicationAsync(int userId, int apartmentId);
    
    /// <summary>
    /// Gets the application status for a user and apartment
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="apartmentId">The apartment ID</param>
    /// <returns>The application if it exists, null otherwise</returns>
    Task<ApartmentApplication?> GetApplicationAsync(int userId, int apartmentId);
}
