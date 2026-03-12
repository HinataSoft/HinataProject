using Microsoft.Extensions.Configuration;
using Npgsql;

namespace VoucherService.Tests.Shared.DatabaseStrategies;

public class RandomPostgresDatabaseStrategy : IDatabaseSetupStrategy
{
    private readonly string connectionString;
    private readonly string database;

    public RandomPostgresDatabaseStrategy()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TESTS_CONNECTION_STRING"));
        builder.Database = $"test_db_temp_{Guid.NewGuid():N}";
        
        database = builder.Database;
        connectionString = builder.ToString();
    }

    public async Task InitializeAsync(IConfiguration configuration)
    {
        var postgresDbBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        postgresDbBuilder.Database = "postgres";

        // Create the database using _connectionString
        await using var connection = new NpgsqlConnection(postgresDbBuilder.ToString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($$"""
                                                      CREATE DATABASE {{database}}
                                                      """, connection);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        var postgresDbBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        postgresDbBuilder.Database = "postgres";

        // Drop the database using _connectionString
        await using var connection = new NpgsqlConnection(postgresDbBuilder.ToString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($$"""
                                                      DROP DATABASE IF EXISTS {{database}} WITH (FORCE)
                                                      """, connection);
        await command.ExecuteNonQueryAsync();
    }

    public string GetConnectionString()
    {
        return connectionString;
    }
}