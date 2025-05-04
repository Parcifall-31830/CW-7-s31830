using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.Exceptions;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[ApiController]
[Route("[controller]")] 
public class TripController(IDbService service) :ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await service.GetTripsAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTripById(
        [FromRoute] int id)
    {
        try
        {
            return Ok(await service.GetTripByIdAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

}