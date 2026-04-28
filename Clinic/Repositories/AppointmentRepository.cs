namespace Clinic.Repositories;

using Microsoft.Data.SqlClient;
using Clinic.DTOs;

public class AppointmentRepository(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")!;

    public async Task<List<AppointmentListDto>> GetAll(
        AppointmentStatus? status,
        string? patientLastName
    )
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + N' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;
        """, connection);

        command.Parameters
            .AddWithValue("@Status", (object?)status?.ToString() ?? DBNull.Value);
        command.Parameters
            .AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);

        await connection.OpenAsync();

        var results = new List<AppointmentListDto>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = Enum.Parse<AppointmentStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
                PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
            });
        }

        return results;
    }


    public async Task<AppointmentDetailsDto?> GetById(int idAppointment)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                a.InternalNotes,
                a.CreatedAt,
                p.FirstName + N' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail,
                p.PhoneNumber AS PatientPhone,
                d.LicenseNumber AS DoctorLicenseNumber,
                s.Name AS DoctorSpecializationName
            FROM dbo.Appointments a
            JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
            JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
            JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
            WHERE a.IdAppointment = @IdAppointment;
        """, connection);

        command.Parameters.AddWithValue("@IdAppointment", idAppointment);

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
            AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
            Status = Enum.Parse<AppointmentStatus>(reader.GetString(reader.GetOrdinal("Status"))),
            Reason = reader.GetString(reader.GetOrdinal("Reason")),
            InternalNotes = reader.IsDBNull(reader.GetOrdinal("InternalNotes"))
                                           ? string.Empty
                                           : reader.GetString(reader.GetOrdinal("InternalNotes")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
            PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
            PatientPhone = reader.GetString(reader.GetOrdinal("PatientPhone")),
            DoctorLicenseNumber = reader.GetString(reader.GetOrdinal("DoctorLicenseNumber")),
            DoctorSpecializationName = reader.GetString(reader.GetOrdinal("DoctorSpecializationName")),
        };
    }


    public async Task<bool> PatientExistsAndActive(int idPatient)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT 1 FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1;
        """, connection);
        command.Parameters.AddWithValue("@IdPatient", idPatient);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task<bool> DoctorExistsAndActive(int idDoctor)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT 1 FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1;
        """, connection);
        command.Parameters.AddWithValue("@IdDoctor", idDoctor);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task<bool> DoctorHasAppointmentAt(int idDoctor, DateTime appointmentDate)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT 1 FROM dbo.Appointments
            WHERE IdDoctor = @IdDoctor
              AND AppointmentDate = @AppointmentDate
              AND Status = 'Scheduled';
        """, connection);
        command.Parameters.AddWithValue("@IdDoctor", idDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", appointmentDate);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task<int> Create(CreateAppointmentRequestDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
            OUTPUT INSERTED.IdAppointment
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, 'Scheduled', @Reason);
        """, connection);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        await connection.OpenAsync();
        return (int)(await command.ExecuteScalarAsync())!;
    }

    public async Task<(AppointmentStatus status, DateTime appointmentDate)?> GetStatusAndDate(int idAppointment)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT Status, AppointmentDate FROM dbo.Appointments WHERE IdAppointment = @IdAppointment;
        """, connection);
        command.Parameters.AddWithValue("@IdAppointment", idAppointment);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return (
            Enum.Parse<AppointmentStatus>(reader.GetString(0)),
            reader.GetDateTime(1)
        );
    }

    public async Task<bool> DoctorHasAppointmentAtExcluding(int idDoctor, DateTime appointmentDate, int excludeIdAppointment)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            SELECT 1 FROM dbo.Appointments
            WHERE IdDoctor = @IdDoctor
              AND AppointmentDate = @AppointmentDate
              AND Status = 'Scheduled'
              AND IdAppointment <> @ExcludeIdAppointment;
        """, connection);
        command.Parameters.AddWithValue("@IdDoctor", idDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", appointmentDate);
        command.Parameters.AddWithValue("@ExcludeIdAppointment", excludeIdAppointment);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync() is not null;
    }

    public async Task Update(int idAppointment, UpdateAppointmentRequestDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            UPDATE dbo.Appointments
            SET IdPatient     = @IdPatient,
                IdDoctor      = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status        = @Status,
                Reason        = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
        """, connection);
        command.Parameters.AddWithValue("@IdAppointment", idAppointment);
        command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@Status", dto.Status.ToString());
        command.Parameters.AddWithValue("@Reason", dto.Reason);
        command.Parameters.AddWithValue("@InternalNotes", (object?)dto.InternalNotes ?? DBNull.Value);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public async Task Delete(int idAppointment)
    {
        using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand("""
            DELETE FROM dbo.Appointments WHERE IdAppointment = @IdAppointment;
        """, connection);
        command.Parameters.AddWithValue("@IdAppointment", idAppointment);
        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}

