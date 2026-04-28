namespace Clinic.Controllers;

using Microsoft.AspNetCore.Mvc;
using Clinic.DTOs;
using Clinic.Services;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(AppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] string? patientLastName = null
    )
    {
        return Ok(await appointmentService.GetAll(status, patientLastName));
    }


    [HttpGet("{idAppointment}")]
    public async Task<IActionResult> Get([FromRoute] int idAppointment)
    {
        var result = await appointmentService.GetById(idAppointment);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequestDto dto)
    {
        var (id, error) = await appointmentService.Create(dto);

        if (error is not null)
            return ErrorStatusCode(error);

        return CreatedAtAction(nameof(Get), new { idAppointment = id }, new { idAppointment = id });
    }

    [HttpPut("{idAppointment}")]
    public async Task<IActionResult> Update(int idAppointment, [FromBody] UpdateAppointmentRequestDto dto)
    {
        var error = await appointmentService.Update(idAppointment, dto);
        if (error is not null)
            return ErrorStatusCode(error);

        return Ok();
    }

    [HttpDelete("{idAppointment}")]
    public async Task<IActionResult> Delete(int idAppointment)
    {
        var error = await appointmentService.Delete(idAppointment);
        if (error is not null)
            return ErrorStatusCode(error);

        return NoContent();
    }

    private IActionResult ErrorStatusCode(ErrorResponseDto dto) =>
        dto.StatusCode switch
        {
            404 => NotFound(new { dto.Error }),
            409 => Conflict(new { dto.Error }),
            _ => BadRequest(new { dto.Error }),
        };
}

