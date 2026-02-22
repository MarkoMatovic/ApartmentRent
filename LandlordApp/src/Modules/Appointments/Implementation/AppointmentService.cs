using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Interfaces;
using Lander.src.Modules.Appointments.Models;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Listings;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lander.src.Modules.Appointments.Implementation
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppointmentsContext _context;
        private readonly ListingsContext _listingsContext;
        private readonly UsersContext _usersContext;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AppointmentService> _logger;
        private readonly Lander.src.Modules.ApartmentApplications.Interfaces.IApplicationApprovalService _approvalService;

        public AppointmentService(
            AppointmentsContext context,
            ListingsContext listingsContext,
            UsersContext usersContext,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AppointmentService> logger,
            Lander.src.Modules.ApartmentApplications.Interfaces.IApplicationApprovalService approvalService)
        {
            _context = context;
            _listingsContext = listingsContext;
            _usersContext = usersContext;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _approvalService = approvalService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userId;
        }

        private Guid GetCurrentUserGuid()
        {
            var userGuidClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userGuidClaim) || !Guid.TryParse(userGuidClaim, out var userGuid))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userGuid;
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto)
        {
            var tenantId = GetCurrentUserId();
            var tenantGuid = GetCurrentUserGuid();

            // Get apartment and landlord info
            var apartment = await _listingsContext.Apartments
                .FirstOrDefaultAsync(a => a.ApartmentId == dto.ApartmentId);

            if (apartment == null)
            {
                throw new ArgumentException("Apartment not found");
            }

            if (!apartment.LandlordId.HasValue)
            {
                throw new ArgumentException("Apartment does not have a landlord assigned");
            }

            // Check if user has an approved application for this apartment
            var hasApprovedApplication = await _approvalService.HasApprovedApplicationAsync(tenantId, dto.ApartmentId);
            if (!hasApprovedApplication)
            {
                throw new UnauthorizedAccessException("You must have an approved application before scheduling a viewing for this apartment");
            }

            // Check if appointment time is in the future
            if (dto.AppointmentDate <= DateTime.Now)
            {
                throw new ArgumentException("Appointment date must be in the future");
            }

            // Check for conflicts
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.ApartmentId == dto.ApartmentId &&
                              a.Status != AppointmentStatus.Cancelled &&
                              a.Status != AppointmentStatus.Rejected &&
                              a.AppointmentDate == dto.AppointmentDate);

            if (hasConflict)
            {
                throw new InvalidOperationException("This time slot is already booked");
            }

            var appointment = new Appointment
            {
                AppointmentGuid = Guid.NewGuid(),
                ApartmentId = dto.ApartmentId,
                TenantId = tenantId,
                LandlordId = apartment.LandlordId.Value,
                AppointmentDate = dto.AppointmentDate,
                Duration = TimeSpan.FromMinutes(30),
                Status = AppointmentStatus.Pending,
                TenantNotes = dto.TenantNotes,
                CreatedByGuid = tenantGuid,
                CreatedDate = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveEntitiesAsync();

            // Get tenant and landlord info for email
            var tenant = await _usersContext.Users.FindAsync(tenantId);
            var landlord = await _usersContext.Users.FindAsync(apartment.LandlordId);

            // Send email to landlord
            if (landlord != null && tenant != null)
            {
                _ = _emailService.SendAppointmentConfirmationEmailAsync(
                    landlord.Email,
                    landlord.FirstName,
                    appointment.AppointmentDate,
                    apartment.Title
                );
            }

            return await MapToDto(appointment);
        }

        public async Task<List<AppointmentDto>> GetMyAppointmentsAsync()
        {
            var userId = GetCurrentUserId();

            var appointments = await _context.Appointments
                .Where(a => a.TenantId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var dtos = new List<AppointmentDto>();
            foreach (var appointment in appointments)
            {
                dtos.Add(await MapToDto(appointment));
            }

            return dtos;
        }

        public async Task<List<AppointmentDto>> GetLandlordAppointmentsAsync()
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("Getting landlord appointments for userId: {UserId}", userId);

            var appointments = await _context.Appointments
                .Where(a => a.LandlordId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} appointments for landlord userId: {UserId}", appointments.Count, userId);

            var dtos = new List<AppointmentDto>();
            foreach (var appointment in appointments)
            {
                dtos.Add(await MapToDto(appointment));
            }

            return dtos;
        }

        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int apartmentId, DateTime date)
        {
            try
            {
                // Get apartment to find landlord
                var apartment = await _listingsContext.Apartments
                    .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);

                if (apartment == null)
                {
                    throw new ArgumentException("Apartment not found");
                }

                var slots = new List<AvailableSlotDto>();
                var slotDuration = 30; // minutes

                // Get landlord availability for given day of week
                var dayOfWeek = date.DayOfWeek;
                List<(TimeSpan start, TimeSpan end)> availabilityWindows = new();

                if (apartment.LandlordId.HasValue)
                {
                    var landlordAvailability = await _context.LandlordAvailabilities
                        .Where(la => la.LandlordId == apartment.LandlordId.Value &&
                                     la.DayOfWeek == dayOfWeek &&
                                     la.IsActive)
                        .ToListAsync();

                    if (landlordAvailability.Any())
                    {
                        availabilityWindows = landlordAvailability
                            .Select(la => (la.StartTime, la.EndTime))
                            .ToList();
                    }
                }

                // Fallback: 9:00 – 17:00 if landlord has no availability set
                if (!availabilityWindows.Any())
                {
                    availabilityWindows.Add((new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)));
                }

                // Get existing booked appointments for this day
                var dayStart = date.Date;
                var dayEnd = date.Date.AddDays(1);
                var bookedAppointments = await _context.Appointments
                    .Where(a => a.ApartmentId == apartmentId &&
                               a.AppointmentDate >= dayStart &&
                               a.AppointmentDate < dayEnd &&
                               a.Status != AppointmentStatus.Cancelled &&
                               a.Status != AppointmentStatus.Rejected)
                    .Select(a => a.AppointmentDate)
                    .ToListAsync();

                var now = DateTime.Now;
                var today = now.Date;

                foreach (var (winStart, winEnd) in availabilityWindows)
                {
                    var currentTime = winStart;
                    while (currentTime.Add(TimeSpan.FromMinutes(slotDuration)) <= winEnd)
                    {
                        var slotTime = new DateTime(date.Year, date.Month, date.Day,
                            currentTime.Hours, currentTime.Minutes, 0, DateTimeKind.Local);

                        // Skip past slots when date is today
                        if (date.Date == today && slotTime <= now)
                        {
                            currentTime = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
                            continue;
                        }

                        var isBooked = bookedAppointments.Any(a => a == slotTime);

                        slots.Add(new AvailableSlotDto
                        {
                            StartTime = slotTime,
                            EndTime = slotTime.AddMinutes(slotDuration),
                            IsAvailable = !isBooked
                        });

                        currentTime = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
                    }
                }

                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating available slots for apartment {ApartmentId}, date {Date}", apartmentId, date);
                throw;
            }
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, UpdateAppointmentStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var userGuid = GetCurrentUserGuid();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException("Appointment not found");
            }

            // Only landlord can update status
            if (appointment.LandlordId != userId)
            {
                throw new UnauthorizedAccessException("Only the landlord can update appointment status");
            }

            appointment.Status = dto.Status;
            appointment.LandlordNotes = dto.LandlordNotes;
            appointment.ModifiedByGuid = userGuid;
            appointment.ModifiedDate = DateTime.UtcNow;

            await _context.SaveEntitiesAsync();

            // Send email to tenant
            var tenant = await _usersContext.Users.FindAsync(appointment.TenantId);
            var apartment = await _listingsContext.Apartments.FindAsync(appointment.ApartmentId);

            if (tenant != null && apartment != null)
            {
                var statusText = dto.Status switch
                {
                    AppointmentStatus.Confirmed => "confirmed",
                    AppointmentStatus.Rejected => "rejected",
                    AppointmentStatus.Cancelled => "cancelled",
                    _ => "updated"
                };

                _ = _emailService.SendAppointmentConfirmationEmailAsync(
                    tenant.Email,
                    tenant.FirstName,
                    appointment.AppointmentDate,
                    apartment.Title
                );
            }

            return await MapToDto(appointment);
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var userId = GetCurrentUserId();
            var userGuid = GetCurrentUserGuid();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException("Appointment not found");
            }

            // Only tenant or landlord can cancel
            if (appointment.TenantId != userId && appointment.LandlordId != userId)
            {
                throw new UnauthorizedAccessException("You don't have permission to cancel this appointment");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.ModifiedByGuid = userGuid;
            appointment.ModifiedDate = DateTime.UtcNow;

            await _context.SaveEntitiesAsync();

            return true;
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return null;

            return await MapToDto(appointment);
        }

        public async Task<List<LandlordAvailabilityDto>> GetMyAvailabilityAsync()
        {
            var landlordId = GetCurrentUserId();

            var availability = await _context.LandlordAvailabilities
                .Where(la => la.LandlordId == landlordId && la.IsActive)
                .OrderBy(la => la.DayOfWeek)
                .ThenBy(la => la.StartTime)
                .ToListAsync();

            return availability.Select(la => new LandlordAvailabilityDto
            {
                AvailabilityId = la.AvailabilityId,
                LandlordId = la.LandlordId,
                DayOfWeek = la.DayOfWeek,
                StartTime = la.StartTime,
                EndTime = la.EndTime,
                IsActive = la.IsActive
            }).ToList();
        }

        public async Task<List<LandlordAvailabilityDto>> SetMyAvailabilityAsync(SetAvailabilityDto dto)
        {
            var landlordId = GetCurrentUserId();

            // Remove existing availability for this landlord
            var existing = await _context.LandlordAvailabilities
                .Where(la => la.LandlordId == landlordId)
                .ToListAsync();

            _context.LandlordAvailabilities.RemoveRange(existing);

            // Add new availability slots
            var newSlots = dto.Slots.Select(s => new LandlordAvailability
            {
                LandlordId = landlordId,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            }).ToList();

            _context.LandlordAvailabilities.AddRange(newSlots);
            await _context.SaveEntitiesAsync();

            _logger.LogInformation("Landlord {LandlordId} set {Count} availability slots", landlordId, newSlots.Count);

            return newSlots.Select(la => new LandlordAvailabilityDto
            {
                AvailabilityId = la.AvailabilityId,
                LandlordId = la.LandlordId,
                DayOfWeek = la.DayOfWeek,
                StartTime = la.StartTime,
                EndTime = la.EndTime,
                IsActive = la.IsActive
            }).ToList();
        }

        private async Task<AppointmentDto> MapToDto(Appointment appointment)
        {
            var apartment = await _listingsContext.Apartments.FindAsync(appointment.ApartmentId);
            var tenant = await _usersContext.Users.FindAsync(appointment.TenantId);
            var landlord = await _usersContext.Users.FindAsync(appointment.LandlordId);

            return new AppointmentDto
            {
                AppointmentId = appointment.AppointmentId,
                AppointmentGuid = appointment.AppointmentGuid,
                ApartmentId = appointment.ApartmentId,
                ApartmentTitle = apartment?.Title,
                ApartmentAddress = apartment?.Address,
                TenantId = appointment.TenantId,
                TenantName = tenant != null ? $"{tenant.FirstName} {tenant.LastName}" : null,
                TenantEmail = tenant?.Email,
                LandlordId = appointment.LandlordId,
                LandlordName = landlord != null ? $"{landlord.FirstName} {landlord.LastName}" : null,
                LandlordEmail = landlord?.Email,
                AppointmentDate = appointment.AppointmentDate,
                Duration = appointment.Duration,
                Status = appointment.Status,
                TenantNotes = appointment.TenantNotes,
                LandlordNotes = appointment.LandlordNotes,
                CreatedDate = appointment.CreatedDate
            };
        }
    }
}
