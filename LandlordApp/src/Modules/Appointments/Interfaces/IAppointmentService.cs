using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Models;

namespace Lander.src.Modules.Appointments.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<List<AppointmentDto>> GetMyAppointmentsAsync();
        Task<List<AppointmentDto>> GetLandlordAppointmentsAsync();
        Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int apartmentId, DateTime date);
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, UpdateAppointmentStatusDto dto);
        Task<bool> CancelAppointmentAsync(int appointmentId);
        Task<AppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
    }
}
