namespace Clinic.Services;

using Clinic.DTOs;
using Clinic.Repositories;


public class AppointmentService(AppointmentRepository appointmentRepository)
{
    public async Task<List<AppointmentListDto>> GetAll(
        AppointmentStatus? status,
        string? patientLastName
    )
    {
        return await appointmentRepository.GetAll(status, patientLastName);
    }


    public async Task<AppointmentDetailsDto?> GetById(int idAppointment)
    {
        return await appointmentRepository.GetById(idAppointment);
    }


    public async Task<(int? id, ErrorResponseDto? error)> Create(CreateAppointmentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, new(400, "Reason cannot be empty."));

        if (dto.Reason.Length > 250)
            return (null, new(400, "Reason cannot exceed 250 characters."));

        if (dto.AppointmentDate <= DateTime.UtcNow)
            return (null, new(400, "Appointment date must be in the future."));

        if (!await appointmentRepository.PatientExistsAndActive(dto.IdPatient))
            return (null, new(400, "Patient does not exist or is not active."));

        if (!await appointmentRepository.DoctorExistsAndActive(dto.IdDoctor))
            return (null, new(400, "Doctor does not exist or is not active."));

        if (await appointmentRepository.DoctorHasAppointmentAt(dto.IdDoctor, dto.AppointmentDate))
            return (null, new(409, "Doctor already has a scheduled appointment at this time."));

        return (await appointmentRepository.Create(dto), null);
    }

    public async Task<ErrorResponseDto?> Update(
            int idAppointment, UpdateAppointmentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return new(400, "Reason cannot be empty.");
        if (dto.Reason.Length > 250)
            return new(400, "Reason cannot exceed 250 characters.");

        var existing = await appointmentRepository.GetStatusAndDate(idAppointment);
        if (existing is null)
            return new(404, "Appointment not found.");

        if (!await appointmentRepository.PatientExistsAndActive(dto.IdPatient))
            return new(400, "Patient does not exist or is not active.");
        if (!await appointmentRepository.DoctorExistsAndActive(dto.IdDoctor))
            return new(400, "Doctor does not exist or is not active.");

        if (existing.Value.status == AppointmentStatus.Completed &&
            dto.AppointmentDate != existing.Value.appointmentDate)
            return new(400, "Cannot reschedule a completed appointment.");

        if (await appointmentRepository.DoctorHasAppointmentAtExcluding(dto.IdDoctor, dto.AppointmentDate, idAppointment))
            return new(409, "Doctor already has a scheduled appointment at this time.");

        await appointmentRepository.Update(idAppointment, dto);
        return null;
    }

    public async Task<ErrorResponseDto?> Delete(int idAppointment)
    {
        var existing = await appointmentRepository.GetStatusAndDate(idAppointment);
        if (existing is null)
            return new(404, "Appointment not found.");
        if (existing.Value.status == AppointmentStatus.Completed)
            return new(409, "Cannot delete a completed appointment.");
        await appointmentRepository.Delete(idAppointment);
        return null;
    }
}

