using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TicketManagement.Server.Data
{
    public class TicketRepository
    {
        private readonly string _connectionString;

        // Public Ticket ID Configuration
        private const string PublicTicketIdPrefix = "NET";
        private const string PublicTicketIdSeparator = "-";
        private const int PublicTicketIdStartNumber = 1001;

        public TicketRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TicketDb")!;
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        public async Task<List<Tickets>> GetTicketsAsync()
        {
            var list = new List<Tickets>();
            const string sql =
                @"SELECT TicketId, PublicTicketId, Title, Description, Category, Department, Assignee, 
                                        CreatedBy, Status, Priority, ResponseDue, DueDate, CreatedAt, UpdatedAt
                                 FROM dbo.Tickets ORDER BY TicketId";

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(
                    new Tickets
                    {
                        TicketId = reader.GetInt32(reader.GetOrdinal("TicketId")),
                        PublicTicketId = reader["PublicTicketId"] as string,
                        Title = reader["Title"] as string,
                        Description = reader["Description"] as string,
                        Category = reader["Category"] as string,
                        Department = reader["Department"] as string,
                        Assignee = reader["Assignee"] as string,
                        CreatedBy = reader["CreatedBy"] as string,
                        Status = reader["Status"] as string,
                        Priority = reader["Priority"] as string,
                        ResponseDue =
                            reader["ResponseDue"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["ResponseDue"]),
                        DueDate =
                            reader["DueDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["DueDate"]),
                        CreatedAt =
                            reader["CreatedAt"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["CreatedAt"]),
                        UpdatedAt =
                            reader["UpdatedAt"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["UpdatedAt"]),
                    }
                );
            }
            return list;
        }

        private async Task<string> GeneratePublicTicketIdAsync()
        {
            // Efficiently get max numeric suffix with SQL
            string like = $"{PublicTicketIdPrefix}{PublicTicketIdSeparator}%";
            const string sql =
                @"
                SELECT MAX(TRY_CAST(SUBSTRING(PublicTicketId, LEN(@prefix) + LEN(@sep) + 1, 50) AS INT))
                FROM dbo.Tickets
                WHERE PublicTicketId LIKE @like";
            int? maxNumber = null;

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@prefix", PublicTicketIdPrefix);
            cmd.Parameters.AddWithValue("@sep", PublicTicketIdSeparator);
            cmd.Parameters.AddWithValue("@like", like);

            var result = await cmd.ExecuteScalarAsync();
            if (result != DBNull.Value && result != null)
            {
                maxNumber = Convert.ToInt32(result);
            }

            int next = (maxNumber ?? (PublicTicketIdStartNumber - 1)) + 1;
            return $"{PublicTicketIdPrefix}{PublicTicketIdSeparator}{next}";
        }

        public async Task<Tickets> InsertAsync(Tickets value)
        {
            // Auto-generate PublicTicketId if empty
            if (string.IsNullOrWhiteSpace(value.PublicTicketId))
            {
                value.PublicTicketId = await GeneratePublicTicketIdAsync();
            }

            // Default timestamps
            value.CreatedAt ??= DateTime.UtcNow;
            value.UpdatedAt ??= DateTime.UtcNow;

            const string sql =
                @"
                INSERT INTO dbo.Tickets
                (PublicTicketId, Title, Description, Category, Department, Assignee, CreatedBy, Status, Priority,
                 ResponseDue, DueDate, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.TicketId
                VALUES
                (@PublicTicketId, @Title, @Description, @Category, @Department, @Assignee, @CreatedBy, @Status, @Priority,
                 @ResponseDue, @DueDate, @CreatedAt, @UpdatedAt);";

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(
                "@PublicTicketId",
                (object?)value.PublicTicketId ?? DBNull.Value
            );
            cmd.Parameters.AddWithValue("@Title", (object?)value.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)value.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Category", (object?)value.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Department", (object?)value.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Assignee", (object?)value.Assignee ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)value.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object?)value.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Priority", (object?)value.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResponseDue", (object?)value.ResponseDue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DueDate", (object?)value.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", (object?)value.CreatedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedAt", (object?)value.UpdatedAt ?? DBNull.Value);

            value.TicketId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return value;
        }

        public async Task<Tickets> UpdateAsync(Tickets value)
        {
            value.UpdatedAt ??= DateTime.UtcNow;

            const string sql =
                @"
                UPDATE dbo.Tickets
                   SET PublicTicketId = @PublicTicketId,
                       Title         = @Title,
                       Description   = @Description,
                       Category      = @Category,
                       Department    = @Department,
                       Assignee      = @Assignee,
                       CreatedBy     = @CreatedBy,
                       Status        = @Status,
                       Priority      = @Priority,
                       ResponseDue   = @ResponseDue,
                       DueDate       = @DueDate,
                       UpdatedAt     = @UpdatedAt
                 WHERE TicketId     = @TicketId;";

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TicketId", value.TicketId);
            cmd.Parameters.AddWithValue(
                "@PublicTicketId",
                (object?)value.PublicTicketId ?? DBNull.Value
            );
            cmd.Parameters.AddWithValue("@Title", (object?)value.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)value.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Category", (object?)value.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Department", (object?)value.Department ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Assignee", (object?)value.Assignee ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", (object?)value.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object?)value.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Priority", (object?)value.Priority ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResponseDue", (object?)value.ResponseDue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DueDate", (object?)value.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedAt", (object?)value.UpdatedAt ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return value;
        }

        public async Task<int> DeleteAsync(int ticketId)
        {
            const string sql = @"DELETE FROM dbo.Tickets WHERE TicketId = @TicketId;";
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TicketId", ticketId);
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
