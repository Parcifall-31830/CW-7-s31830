using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WebApplication2.Exceptions;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services;

public interface  IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<IEnumerable<ClientTripWithInfoDTO>>GetAllTripsByClientIdAsync(int id);
    public Task<int> AddClientDB(ClientGetDTO cgd);

}

public class DbService(IConfiguration config): IDbService
{
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        
        var sql= @"Select t.IdTrip,t.Name,t.Description,t.DateFrom,t.DateTo,t.MaxPeople,c.Name from Trip t
           inner join Country_Trip ct on t.IdTrip = ct.IdTrip
           inner join Country c on ct.IdCountry = c.IdCountry
        ";
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
                MaxPeople = reader.GetInt32(5),
                Country = reader.GetString(6),  
            });
        }
        
        return result;
        
    }

    public async Task<IEnumerable<ClientTripWithInfoDTO>> GetAllTripsByClientIdAsync(int clientId)
    {
        var result = new List<ClientTripWithInfoDTO>();
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);

        // 1. Sprawdzenie czy klient istnieje
        var checkClientSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId";
        await using (var checkCmd = new SqlCommand(checkClientSql, connection))
        {
            checkCmd.Parameters.AddWithValue("@ClientId", clientId);
            await connection.OpenAsync();
            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
            if (!exists)
            {
                throw new NotFoundException($"Client with ID {clientId} does not exist.");
            }
            await connection.CloseAsync();
        }

        // 2. Pobranie wycieczek + info o rejestracji/płatności
        var sql = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
               ct.RegisteredAt, ct.PaymentDate
        FROM Client_Trip ct
        JOIN Trip t ON t.IdTrip = ct.IdTrip
        WHERE ct.IdClient = @ClientId";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ClientId", clientId);
        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripWithInfoDTO()
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        return result;
    }

    public async Task<int> AddClientDB(ClientGetDTO client)
    {
        if (
            string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Telephone) ||
            string.IsNullOrWhiteSpace(client.Pesel)
        )
        {
            throw new BadRequestException("Please provide valid client data.");
        }

        if (client.Pesel.Length != 11)
        {
            throw new BadRequestException("Pesel must be 11 characters long.");
        }

        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var getIdSql = "SELECT MAX(IdClient) + 1 FROM Client";
        await using var getIdCmd = new SqlCommand(getIdSql, connection);
        var newId = (int)await getIdCmd.ExecuteScalarAsync();

        
        var sql = @"INSERT INTO Client(FirstName, LastName, Email, Telephone, Pesel) 
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        await command.ExecuteNonQueryAsync();

        return newId;
    }



}