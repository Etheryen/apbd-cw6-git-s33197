namespace Clinic.DTOs;

using System.Text.Json.Serialization;

public class UpdateAppointmentRequestDto
{
    public int IdPatient { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppointmentStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
}

