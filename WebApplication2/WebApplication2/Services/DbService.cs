using Microsoft.Data.SqlClient;
using WebApplication2.Exceptions;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services;

public interface  IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<TripGetDTO> GetTripByIdAsync(int id);
}

public class DbService(IConfiguration config): IDbService
{
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        
        var sql= "Select IdTrip,Name,Description,DateFrom,DateTo,MaxPeople from Trip";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5)
            });
        }
        
        return result;
        
    }

    public async Task<TripGetDTO> GetTripByIdAsync(int id)
    {
        var connectionString = config.GetConnectionString("Default");
        await using var connection = new SqlConnection(connectionString);
        
        const string sql = "Select IdTrip,Name,Description,DateFrom,DateTo,MaxPeople from Trip where IdTrip=@id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new NotFoundException("Trip not found");
        }

        return new TripGetDTO()
        {
            IdTrip = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            DateFrom = reader.GetDateTime(3),
            DateTo = reader.GetDateTime(4),
            MaxPeople = reader.GetInt32(5)
        };
    }

}