using Microsoft.AspNetCore.Mvc;
using WebApplication2.Models.DTOs;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[ApiController]
[Route("[Controller]")]
public class clientsController(IDbService service) :ControllerBase
{
    [HttpGet]
    [Route("{id}/trips")]
    public async Task<IActionResult> GetAllClientTripsByClientId(int id)
    {
        return Ok(await service.GetAllTripsByClientIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> AddClient(
    [FromBody] ClientGetDTO cgd)
    {
        var newClientId = await service.AddClientDB(cgd);
            return CreatedAtAction(nameof(GetAllClientTripsByClientId), new { id = newClientId }, new { Id = newClientId });
    }
}