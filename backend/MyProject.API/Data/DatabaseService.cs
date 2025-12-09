using MySqlConnector;
using MyProject.API.Models;

namespace MyProject.API.Data;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new Exception("Database connection string not found");
    }

    public async Task<UserInfo?> GetUserByFirebaseUidAsync(string firebaseUid)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT u.id, u.username, u.email, u.full_name, u.role, u.tlp_rating, u.firebase_uid, u.company_name
            FROM Users u
            WHERE u.firebase_uid = @firebaseUid
            LIMIT 1";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@firebaseUid", firebaseUid);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new UserInfo
            {
                Uid = firebaseUid,
                Email = reader.GetString("email"),
                DisplayName = reader.GetString("full_name"),
                Role = reader.GetString("role"),
                TlpRating = reader.GetString("tlp_rating"),
                CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString("company_name")
            };
        }

        return null;
    }

    public async Task<UserInfo?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT u.id, u.username, u.email, u.full_name, u.role, u.tlp_rating, u.firebase_uid, u.company_name
            FROM Users u
            WHERE u.username = @identifier OR u.email = @identifier
            LIMIT 1";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@identifier", usernameOrEmail);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var firebaseUid = reader.IsDBNull(reader.GetOrdinal("firebase_uid")) ? null : reader.GetString("firebase_uid");
            return new UserInfo
            {
                Uid = firebaseUid ?? $"test_{reader.GetInt32("id")}",
                Email = reader.GetString("email"),
                DisplayName = reader.GetString("full_name"),
                Role = reader.GetString("role"),
                TlpRating = reader.GetString("tlp_rating"),
                CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString("company_name")
            };
        }

        return null;
    }

    public async Task<UserInfo?> AuthenticateUserFromTokenAsync(string token)
    {
        // Check if this is a test token
        if (token.StartsWith("dGVzdDo=")) // Base64 for "test:"
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                if (parts.Length >= 3 && parts[0] == "test")
                {
                    var userId = int.Parse(parts[1]);
                    var email = parts[2];
                    
                    using var connection = new MySqlConnection(_connectionString);
                    await connection.OpenAsync();
                    
                    var userQuery = @"
                        SELECT u.id, u.username, u.email, u.full_name, u.role, u.tlp_rating, u.firebase_uid, u.company_name
                        FROM Users u
                        WHERE u.id = @userId OR u.email = @email
                        LIMIT 1";
                    
                    using var userCommand = new MySqlCommand(userQuery, connection);
                    userCommand.Parameters.AddWithValue("@userId", userId);
                    userCommand.Parameters.AddWithValue("@email", email);
                    
                    using var reader = await userCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var firebaseUid = reader.IsDBNull(reader.GetOrdinal("firebase_uid")) ? null : reader.GetString("firebase_uid");
                        var userInfo = new UserInfo
                        {
                            Uid = firebaseUid ?? $"test_{userId}",
                            Email = reader.GetString("email"),
                            DisplayName = reader.GetString("full_name"),
                            Role = reader.GetString("role"),
                            TlpRating = reader.GetString("tlp_rating"),
                            CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString("company_name"),
                            EmailVerified = true
                        };
                        await reader.CloseAsync();
                        await connection.CloseAsync();
                        return userInfo;
                    }
                    await reader.CloseAsync();
                    await connection.CloseAsync();
                }
            }
            catch
            {
                // Invalid test token, return null to try Firebase
            }
        }
        
        return null; // Not a test token, caller should use Firebase
    }

    public async Task<bool> CreateOrUpdateUserFromFirebaseAsync(string firebaseUid, string email, string? displayName, string? role = null, int? companyId = null)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check if user exists by email
        var checkQuery = "SELECT id FROM Users WHERE email = @email LIMIT 1";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@email", email);

        var userId = await checkCommand.ExecuteScalarAsync();

        if (userId != null)
        {
            // Update existing user with Firebase UID
            var updateQuery = @"
                UPDATE Users 
                SET firebase_uid = @firebaseUid, 
                    full_name = COALESCE(@displayName, full_name),
                    updated_at = NOW()
                WHERE email = @email";

            using var updateCommand = new MySqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@firebaseUid", firebaseUid);
            updateCommand.Parameters.AddWithValue("@email", email);
            updateCommand.Parameters.AddWithValue("@displayName", displayName ?? (object)DBNull.Value);

            await updateCommand.ExecuteNonQueryAsync();
            return true;
        }
        else
        {
            // Validate role
            var validRole = role?.ToLower();
            if (string.IsNullOrEmpty(validRole) || 
                (validRole != "admin" && validRole != "manager" && validRole != "employee"))
            {
                validRole = "employee"; // Default to employee
            }

            // Get company name if companyId is provided
            string? companyName = null;
            if (companyId.HasValue && companyId.Value > 0)
            {
                var companyQuery = "SELECT name FROM Companies WHERE id = @companyId LIMIT 1";
                using var companyCommand = new MySqlCommand(companyQuery, connection);
                companyCommand.Parameters.AddWithValue("@companyId", companyId.Value);
                var companyNameResult = await companyCommand.ExecuteScalarAsync();
                if (companyNameResult != null)
                {
                    companyName = companyNameResult.ToString();
                }
            }

            // Create new user
            var insertQuery = @"
                INSERT INTO Users (username, email, password_hash, full_name, role, tlp_rating, firebase_uid, company_name, is_active)
                VALUES (@username, @email, @passwordHash, @fullName, @role, 'GREEN', @firebaseUid, @companyName, TRUE)";

            using var insertCommand = new MySqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@username", email.Split('@')[0]);
            insertCommand.Parameters.AddWithValue("@email", email);
            insertCommand.Parameters.AddWithValue("@passwordHash", "firebase_auth"); // Placeholder since Firebase handles auth
            insertCommand.Parameters.AddWithValue("@fullName", displayName ?? email.Split('@')[0]);
            insertCommand.Parameters.AddWithValue("@role", validRole);
            insertCommand.Parameters.AddWithValue("@firebaseUid", firebaseUid);
            insertCommand.Parameters.AddWithValue("@companyName", companyName ?? (object)DBNull.Value);

            await insertCommand.ExecuteNonQueryAsync();

            // Link user to company if companyId is provided
            if (companyId.HasValue && companyId.Value > 0)
            {
                var newUserId = (int)insertCommand.LastInsertedId;
                var linkQuery = @"
                    INSERT INTO UserCompanies (user_id, company_id, is_primary)
                    VALUES (@userId, @companyId, TRUE)
                    ON DUPLICATE KEY UPDATE is_primary = TRUE";
                
                using var linkCommand = new MySqlCommand(linkQuery, connection);
                linkCommand.Parameters.AddWithValue("@userId", newUserId);
                linkCommand.Parameters.AddWithValue("@companyId", companyId.Value);
                await linkCommand.ExecuteNonQueryAsync();
            }

            return true;
        }
    }
}


