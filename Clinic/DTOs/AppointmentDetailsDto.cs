namespace Clinic.DTOs;

public class AppointmentDetailsDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public AppointmentStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string DoctorLicenseNumber { get; set; } = string.Empty;
    public string DoctorSpecializationName { get; set; } = string.Empty;
    public string InternalNotes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

}

public enum AppointmentStatus
{
    Scheduled,
    Completed,
    Cancelled
}
