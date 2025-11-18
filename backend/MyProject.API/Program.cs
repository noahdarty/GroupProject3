using MyProject.API.Services;
using MyProject.API.Data;
using MyProject.API.Models;
using MySqlConnector;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
});

// Add custom services
builder.Services.AddHttpClient<FirebaseService>();
builder.Services.AddScoped<DatabaseService>();

// Add CORS to allow frontend requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080", "http://127.0.0.1:8080", "http://127.0.0.1:5500", "http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
app.UseHttpsRedirection();
}

// Helper function to authenticate user from token (handles both test and Firebase tokens)
async Task<UserInfo?> AuthenticateUserAsync(string token, FirebaseService firebaseService, DatabaseService dbService)
{
    // Try test token first
    var testUser = await dbService.AuthenticateUserFromTokenAsync(token);
    if (testUser != null)
    {
        return testUser;
    }
    
    // Try Firebase token
    try
    {
        var firebaseUser = await firebaseService.VerifyTokenAsync(token);
        return await dbService.GetUserByFirebaseUidAsync(firebaseUser.Uid);
    }
    catch
    {
        return null;
    }
}

// Authentication endpoints
app.MapPost("/api/auth/verify-token", async (VerifyTokenRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        if (request == null || string.IsNullOrEmpty(request.IdToken))
        {
            return Results.BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Token is required"
            });
        }

        // Verify Firebase token
        var firebaseUser = await firebaseService.VerifyTokenAsync(request.IdToken);
        
        // Log email verification status for debugging
        Console.WriteLine($"User {firebaseUser.Email} - EmailVerified: {firebaseUser.EmailVerified}");
        
        // Check if email is verified
        if (!firebaseUser.EmailVerified)
        {
            return Results.BadRequest(new AuthResponse
            {
                Success = false,
                Message = $"Email address not verified. Current status: {firebaseUser.EmailVerified}. Please check your email and verify your account, then try logging in again."
            });
        }
        
        // Get or create user in database
        var dbUser = await dbService.GetUserByFirebaseUidAsync(firebaseUser.Uid);
        
        if (dbUser == null)
        {
            // Create new user in database with role and company if provided (during signup)
            await dbService.CreateOrUpdateUserFromFirebaseAsync(
                firebaseUser.Uid,
                firebaseUser.Email,
                firebaseUser.DisplayName,
                request.Role,
                request.CompanyId
            );
            
            // Get the newly created user
            dbUser = await dbService.GetUserByFirebaseUidAsync(firebaseUser.Uid);
        }

        // Set email verified status
        if (dbUser != null)
        {
            dbUser.EmailVerified = firebaseUser.EmailVerified;
        }

        return Results.Ok(new AuthResponse
        {
            Success = true,
            Message = "Token verified successfully",
            User = dbUser,
            IdToken = request.IdToken
        });
    }
    catch (Exception ex)
    {
        // Log the full exception for debugging
        Console.WriteLine($"Error in verify-token: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        return Results.BadRequest(new AuthResponse
        {
            Success = false,
            Message = $"Authentication error: {ex.Message}"
        });
    }
})
.WithName("VerifyToken")
.WithOpenApi();

// Protected endpoint example (requires authentication)
app.MapGet("/api/auth/me", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(dbUser);
    }
    catch (Exception)
    {
        return Results.Unauthorized();
    }
})
.WithName("GetCurrentUser")
.WithOpenApi();

// Database test endpoint
app.MapGet("/api/database/test", async (DatabaseService dbService) =>
{
    try
    {
        // Simple test query to check database connection
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = "SELECT COUNT(*) as user_count FROM Users";
        using var command = new MySqlCommand(query, connection);
        var result = await command.ExecuteScalarAsync();
        
        await connection.CloseAsync();
        
        return Results.Ok(new
        {
            Success = true,
            Message = "Database connection successful",
            UserCount = result,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = $"Database connection failed: {ex.Message}",
            Timestamp = DateTime.UtcNow
        });
    }
})
.WithName("TestDatabaseConnection")
.WithOpenApi();

// Get all users endpoint (for testing)
app.MapGet("/api/users", async (DatabaseService dbService) =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = "SELECT id, username, email, full_name, role, tlp_rating, is_active, created_at FROM Users";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var users = new List<object>();
        while (await reader.ReadAsync())
        {
            users.Add(new
            {
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Email = reader.GetString("email"),
                FullName = reader.GetString("full_name"),
                Role = reader.GetString("role"),
                TlpRating = reader.GetString("tlp_rating"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new
        {
            Success = true,
            Count = users.Count,
            Users = users
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = $"Error fetching users: {ex.Message}"
        });
    }
})
.WithName("GetAllUsers")
.WithOpenApi();

// Get all vendors endpoint (for testing)
app.MapGet("/api/vendors", async (DatabaseService dbService) =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = "SELECT id, name, vendor_type, description, created_at FROM Vendors";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var vendors = new List<object>();
        while (await reader.ReadAsync())
        {
            vendors.Add(new
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                VendorType = reader.GetString("vendor_type"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new
        {
            Success = true,
            Count = vendors.Count,
            Vendors = vendors
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = $"Error fetching vendors: {ex.Message}"
        });
    }
})
.WithName("GetAllVendors")
.WithOpenApi();

// Get user's company (or create one)
app.MapGet("/api/user/company", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            return Results.Unauthorized();
        }

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get user's company - need to find user ID from email or UID
        var userIdQuery = "SELECT id FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        var userId = await userIdCommand.ExecuteScalarAsync();

        if (userId == null)
        {
            await connection.CloseAsync();
            return Results.NotFound(new { message = "User not found" });
        }

        // Get user's company
        var query = @"
            SELECT c.id, c.name, c.description, c.industry, c.created_at, c.updated_at
            FROM Companies c
            INNER JOIN UserCompanies uc ON c.id = uc.company_id
            WHERE uc.user_id = @userId
            LIMIT 1";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var company = new
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString("industry"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Company = company });
        }

        await connection.CloseAsync();

        // No company found - user needs to select one
        return Results.Ok(new { Success = true, Company = (object?)null });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetUserCompany")
.WithOpenApi();

// Get user's selected vendors
app.MapGet("/api/user/vendors", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            Console.WriteLine("GET /api/user/vendors: User authentication failed");
            return Results.Unauthorized();
        }

        Console.WriteLine($"GET /api/user/vendors: Authenticated user {dbUser.Email}");

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get user's company - find user ID from email or UID
        var userIdQuery = "SELECT id FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        var userId = await userIdCommand.ExecuteScalarAsync();

        if (userId == null)
        {
            Console.WriteLine($"GET /api/user/vendors: User ID not found for {dbUser.Email}");
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vendors = new List<object>() });
        }

        Console.WriteLine($"GET /api/user/vendors: Found user ID {userId}");

        // Get user's company
        var companyQuery = @"
            SELECT uc.company_id
            FROM UserCompanies uc
            WHERE uc.user_id = @userId
            LIMIT 1";

        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", userId);
        var companyId = await companyCommand.ExecuteScalarAsync();

        if (companyId == null)
        {
            Console.WriteLine($"GET /api/user/vendors: No company found for user ID {userId}");
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vendors = new List<object>() });
        }

        Console.WriteLine($"GET /api/user/vendors: Found company ID {companyId}");

        // Get company's vendors - one row per vendor
        var vendorsQuery = @"
            SELECT cv.vendor_id, cv.use_case_description, cv.is_active,
                   v.name AS vendor_name, v.vendor_type, v.description AS vendor_description
            FROM CompanyVendors cv
            INNER JOIN Vendors v ON cv.vendor_id = v.id
            WHERE cv.company_id = @companyId AND cv.is_active = TRUE
            ORDER BY v.name";

        var vendors = new List<object>();
        using (var vendorsCommand = new MySqlCommand(vendorsQuery, connection))
        {
            vendorsCommand.Parameters.AddWithValue("@companyId", companyId);
            using var reader = await vendorsCommand.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var vendorId = reader.GetInt32("vendor_id");
                var useCase = reader.IsDBNull(reader.GetOrdinal("use_case_description")) 
                    ? null 
                    : reader.GetString("use_case_description");
                var isActive = reader.GetBoolean("is_active");
                
                vendors.Add(new
                {
                    VendorId = vendorId,
                    VendorName = reader.GetString("vendor_name"),
                    VendorType = reader.GetString("vendor_type"),
                    VendorDescription = reader.IsDBNull(reader.GetOrdinal("vendor_description")) 
                        ? null 
                        : reader.GetString("vendor_description"),
                    UseCaseDescription = useCase,
                    IsActive = isActive
                });
            }
            
            Console.WriteLine($"GET /api/user/vendors: Found {vendors.Count} vendor(s) for company {companyId}");
        }

        await connection.CloseAsync();
        
        Console.WriteLine($"GET /api/user/vendors: Returning {vendors.Count} vendors");
        return Results.Ok(new { Success = true, Vendors = vendors });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"GET /api/user/vendors: Exception: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetUserVendors")
.WithOpenApi();

// Save vendor selections
app.MapPost("/api/user/vendors", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            return Results.Unauthorized();
        }

        // Read request body manually to ensure we get the data
        VendorSelectionRequest? vendorRequest = null;
        try
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            
            Console.WriteLine($"Raw request body: {requestBody}");

            // Parse the JSON manually with case-insensitive property names
            vendorRequest = System.Text.Json.JsonSerializer.Deserialize<VendorSelectionRequest>(
                requestBody,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading/deserializing request: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.BadRequest(new { Success = false, Message = $"Invalid request format: {ex.Message}" });
        }

        if (vendorRequest == null)
        {
            Console.WriteLine("ERROR: vendorRequest is null after deserialization");
            return Results.BadRequest(new { Success = false, Message = "Request body is required" });
        }

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get user's company - find user ID from email or UID
        var userIdQuery = "SELECT id FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        var userId = await userIdCommand.ExecuteScalarAsync();

        if (userId == null)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vendors = new List<object>() });
        }

        // Get user's company
        var companyQuery = @"
            SELECT uc.company_id
            FROM UserCompanies uc
            WHERE uc.user_id = @userId
            LIMIT 1";

        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", userId);
        var companyId = await companyCommand.ExecuteScalarAsync();

        if (companyId == null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "User has no company assigned" });
        }

        // Log received vendor request for debugging
        Console.WriteLine($"Received vendor save request for company {companyId}:");
        Console.WriteLine($"  VendorIds count: {vendorRequest.VendorIds?.Count ?? 0}");
        if (vendorRequest.VendorIds != null && vendorRequest.VendorIds.Count > 0)
        {
            Console.WriteLine($"  VendorIds: {string.Join(", ", vendorRequest.VendorIds)}");
        }
        else
        {
            Console.WriteLine("  WARNING: VendorIds is null or empty!");
        }

        // Delete all existing rows for this company
        var deleteQuery = "DELETE FROM CompanyVendors WHERE company_id = @companyId";
        using var deleteCommand = new MySqlCommand(deleteQuery, connection);
        deleteCommand.Parameters.AddWithValue("@companyId", companyId);
        var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
        Console.WriteLine($"  Deleted {deletedRows} existing row(s) for company {companyId}");

        // Insert one row per vendor
        if (vendorRequest.VendorIds != null && vendorRequest.VendorIds.Count > 0)
        {
            var insertQuery = @"
                INSERT INTO CompanyVendors (company_id, vendor_id, use_case_description, is_active)
                VALUES (@companyId, @vendorId, @useCase, TRUE)";
            
            int insertedCount = 0;
            foreach (var vendorId in vendorRequest.VendorIds)
            {
                var useCase = vendorRequest.UseCaseDescriptions?.ContainsKey(vendorId) == true 
                    ? vendorRequest.UseCaseDescriptions[vendorId] 
                    : null;
                
                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@companyId", companyId);
                insertCommand.Parameters.AddWithValue("@vendorId", vendorId);
                insertCommand.Parameters.AddWithValue("@useCase", useCase ?? (object)DBNull.Value);
                
                await insertCommand.ExecuteNonQueryAsync();
                insertedCount++;
            }
            
            Console.WriteLine($"  Inserted {insertedCount} row(s) into CompanyVendors for company {companyId}");
        }
        else
        {
            Console.WriteLine("  No vendors to insert (VendorIds is empty)");
        }

        await connection.CloseAsync();
        return Results.Ok(new { Success = true, Message = "Vendors saved successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("SaveUserVendors")
.WithOpenApi();

// Get all companies endpoint
app.MapGet("/api/companies", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = "SELECT id, name, description, industry, created_at, updated_at FROM Companies ORDER BY name";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var companies = new List<object>();
        while (await reader.ReadAsync())
        {
            companies.Add(new
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString("industry"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = companies.Count, Companies = companies });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllCompanies")
.WithOpenApi();

// Create company endpoint
app.MapPost("/api/companies", async (HttpRequest request, CreateCompanyRequest companyRequest, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(companyRequest.Name))
        {
            return Results.BadRequest(new { Success = false, Message = "Company name is required" });
        }

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Check if company already exists (case-insensitive)
        var checkQuery = "SELECT id, name, description, industry, created_at, updated_at FROM Companies WHERE LOWER(name) = LOWER(@name) LIMIT 1";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@name", companyRequest.Name);
        using var checkReader = await checkCommand.ExecuteReaderAsync();
        
        if (await checkReader.ReadAsync())
        {
            // Company already exists, return it
            var existingCompany = new
            {
                Id = checkReader.GetInt32("id"),
                Name = checkReader.GetString("name"),
                Description = checkReader.IsDBNull(checkReader.GetOrdinal("description")) ? null : checkReader.GetString("description"),
                Industry = checkReader.IsDBNull(checkReader.GetOrdinal("industry")) ? null : checkReader.GetString("industry"),
                CreatedAt = checkReader.GetDateTime("created_at"),
                UpdatedAt = checkReader.GetDateTime("updated_at")
            };
            await checkReader.CloseAsync();
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Company = existingCompany, Message = "Company already exists" });
        }
        
        await checkReader.CloseAsync();

        // Company doesn't exist, create it
        var insertQuery = @"
            INSERT INTO Companies (name, description, industry)
            VALUES (@name, @description, @industry)";
        
        using var insertCommand = new MySqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@name", companyRequest.Name);
        insertCommand.Parameters.AddWithValue("@description", companyRequest.Description ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@industry", companyRequest.Industry ?? (object)DBNull.Value);
        
        await insertCommand.ExecuteNonQueryAsync();
        var companyId = (int)insertCommand.LastInsertedId;

        await connection.CloseAsync();

        // Return the new company
        await connection.OpenAsync();
        var getCompanyQuery = "SELECT id, name, description, industry, created_at, updated_at FROM Companies WHERE id = @companyId";
        using var getCompanyCommand = new MySqlCommand(getCompanyQuery, connection);
        getCompanyCommand.Parameters.AddWithValue("@companyId", companyId);
        using var getCompanyReader = await getCompanyCommand.ExecuteReaderAsync();
        
        if (await getCompanyReader.ReadAsync())
        {
            var newCompany = new
            {
                Id = getCompanyReader.GetInt32("id"),
                Name = getCompanyReader.GetString("name"),
                Description = getCompanyReader.IsDBNull(getCompanyReader.GetOrdinal("description")) ? null : getCompanyReader.GetString("description"),
                Industry = getCompanyReader.IsDBNull(getCompanyReader.GetOrdinal("industry")) ? null : getCompanyReader.GetString("industry"),
                CreatedAt = getCompanyReader.GetDateTime("created_at"),
                UpdatedAt = getCompanyReader.GetDateTime("updated_at")
            };
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Company = newCompany });
        }

        await connection.CloseAsync();
        return Results.BadRequest(new { Success = false, Message = "Failed to create company" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("CreateCompany")
.WithOpenApi();

// Update user with company and role
app.MapPost("/api/user/update", async (HttpRequest request, UpdateUserRequest updateRequest, FirebaseService firebaseService, DatabaseService dbService) =>
{
    try
    {
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        var token = authHeader.Substring(7);
        var dbUser = await AuthenticateUserAsync(token, firebaseService, dbService);

        if (dbUser == null)
        {
            return Results.Unauthorized();
        }

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get user ID
        var userIdQuery = "SELECT id FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        var userId = await userIdCommand.ExecuteScalarAsync();

        if (userId == null)
        {
            return Results.NotFound(new { message = "User not found" });
        }

        // Update user role
        if (!string.IsNullOrEmpty(updateRequest.Role))
        {
            var updateRoleQuery = "UPDATE Users SET role = @role WHERE id = @userId";
            using var updateRoleCommand = new MySqlCommand(updateRoleQuery, connection);
            updateRoleCommand.Parameters.AddWithValue("@role", updateRequest.Role);
            updateRoleCommand.Parameters.AddWithValue("@userId", userId);
            await updateRoleCommand.ExecuteNonQueryAsync();
        }

        // Link user to company and store company name
        if (updateRequest.CompanyId > 0)
        {
            // Get company name
            var companyQuery = "SELECT name FROM Companies WHERE id = @companyId LIMIT 1";
            using var companyCommand = new MySqlCommand(companyQuery, connection);
            companyCommand.Parameters.AddWithValue("@companyId", updateRequest.CompanyId);
            var companyName = await companyCommand.ExecuteScalarAsync() as string;

            if (!string.IsNullOrEmpty(companyName))
            {
                // Update user with company name
                var updateCompanyNameQuery = "UPDATE Users SET company_name = @companyName WHERE id = @userId";
                using var updateCompanyNameCommand = new MySqlCommand(updateCompanyNameQuery, connection);
                updateCompanyNameCommand.Parameters.AddWithValue("@companyName", companyName);
                updateCompanyNameCommand.Parameters.AddWithValue("@userId", userId);
                await updateCompanyNameCommand.ExecuteNonQueryAsync();
            }

            // Check if already linked
            var checkLinkQuery = "SELECT id FROM UserCompanies WHERE user_id = @userId AND company_id = @companyId LIMIT 1";
            using var checkLinkCommand = new MySqlCommand(checkLinkQuery, connection);
            checkLinkCommand.Parameters.AddWithValue("@userId", userId);
            checkLinkCommand.Parameters.AddWithValue("@companyId", updateRequest.CompanyId);
            var existingLink = await checkLinkCommand.ExecuteScalarAsync();

            if (existingLink == null)
            {
                var linkQuery = @"
                    INSERT INTO UserCompanies (user_id, company_id, is_primary)
                    VALUES (@userId, @companyId, TRUE)";
                
                using var linkCommand = new MySqlCommand(linkQuery, connection);
                linkCommand.Parameters.AddWithValue("@userId", userId);
                linkCommand.Parameters.AddWithValue("@companyId", updateRequest.CompanyId);
                await linkCommand.ExecuteNonQueryAsync();
            }
        }

        await connection.CloseAsync();

        // Get updated user info
        var updatedUser = await dbService.GetUserByFirebaseUidAsync(dbUser.Uid);
        
        return Results.Ok(new { 
            Success = true, 
            Message = "User updated successfully",
            User = updatedUser
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("UpdateUser")
.WithOpenApi();

// Get all vulnerabilities endpoint
app.MapGet("/api/vulnerabilities", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, cve_id, title, description, source, source_url, published_date, 
                     severity_score, severity_level, affected_products, vendor_id, is_duplicate, 
                     duplicate_of_id, created_at, updated_at 
                     FROM Vulnerabilities 
                     ORDER BY created_at DESC 
                     LIMIT 100";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var vulnerabilities = new List<object>();
        while (await reader.ReadAsync())
        {
            vulnerabilities.Add(new
            {
                Id = reader.GetInt32("id"),
                CveId = reader.IsDBNull(reader.GetOrdinal("cve_id")) ? null : reader.GetString("cve_id"),
                Title = reader.GetString("title"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Source = reader.GetString("source"),
                SourceUrl = reader.IsDBNull(reader.GetOrdinal("source_url")) ? null : reader.GetString("source_url"),
                PublishedDate = reader.IsDBNull(reader.GetOrdinal("published_date")) ? null : reader.GetDateTime("published_date").ToString("yyyy-MM-dd"),
                SeverityScore = reader.IsDBNull(reader.GetOrdinal("severity_score")) ? (decimal?)null : reader.GetDecimal("severity_score"),
                SeverityLevel = reader.GetString("severity_level"),
                AffectedProducts = reader.IsDBNull(reader.GetOrdinal("affected_products")) ? null : reader.GetString("affected_products"),
                VendorId = reader.IsDBNull(reader.GetOrdinal("vendor_id")) ? (int?)null : reader.GetInt32("vendor_id"),
                IsDuplicate = reader.GetBoolean("is_duplicate"),
                DuplicateOfId = reader.IsDBNull(reader.GetOrdinal("duplicate_of_id")) ? (int?)null : reader.GetInt32("duplicate_of_id"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = vulnerabilities.Count, Vulnerabilities = vulnerabilities });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllVulnerabilities")
.WithOpenApi();

// Ingest vulnerability endpoint
app.MapPost("/api/vulnerabilities/ingest", async (VulnerabilityIngestRequest request) =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Check for duplicates by CVE ID first
        int? existingId = null;
        bool isDuplicate = false;

        if (!string.IsNullOrEmpty(request.CveId))
        {
            var checkCveQuery = "SELECT id FROM Vulnerabilities WHERE cve_id = @cveId LIMIT 1";
            using var checkCveCommand = new MySqlCommand(checkCveQuery, connection);
            checkCveCommand.Parameters.AddWithValue("@cveId", request.CveId);
            var result = await checkCveCommand.ExecuteScalarAsync();
            if (result != null)
            {
                existingId = Convert.ToInt32(result);
                isDuplicate = true;
            }
        }

        // If no CVE match, check for similar title/description
        if (!isDuplicate)
        {
            var checkSimilarQuery = @"
                SELECT id FROM Vulnerabilities 
                WHERE LOWER(title) = LOWER(@title)
                   OR (description IS NOT NULL AND @description IS NOT NULL 
                       AND LOWER(description) = LOWER(@description))
                LIMIT 1";
            using var checkSimilarCommand = new MySqlCommand(checkSimilarQuery, connection);
            checkSimilarCommand.Parameters.AddWithValue("@title", request.Title);
            checkSimilarCommand.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);
            var similarResult = await checkSimilarCommand.ExecuteScalarAsync();
            if (similarResult != null)
            {
                existingId = Convert.ToInt32(similarResult);
                isDuplicate = true;
            }
        }

        if (isDuplicate && existingId.HasValue)
        {
            // Mark as duplicate
            var updateDuplicateQuery = @"
                UPDATE Vulnerabilities 
                SET is_duplicate = TRUE, duplicate_of_id = @duplicateOfId
                WHERE id = @id";
            using var updateDuplicateCommand = new MySqlCommand(updateDuplicateQuery, connection);
            updateDuplicateCommand.Parameters.AddWithValue("@id", existingId);
            updateDuplicateCommand.Parameters.AddWithValue("@duplicateOfId", existingId);
            await updateDuplicateCommand.ExecuteNonQueryAsync();

            await connection.CloseAsync();
            return Results.Ok(new VulnerabilityIngestResponse
            {
                Success = true,
                Message = "Vulnerability is a duplicate",
                IsDuplicate = true,
                DuplicateOfId = existingId
            });
        }

        // Insert new vulnerability
        var rawDataJson = request.RawData != null 
            ? System.Text.Json.JsonSerializer.Serialize(request.RawData) 
            : null;

        var insertQuery = @"
            INSERT INTO Vulnerabilities (
                cve_id, title, description, source, source_url, published_date,
                severity_score, severity_level, affected_products, vendor_id, raw_data, is_duplicate
            )
            VALUES (
                @cveId, @title, @description, @source, @sourceUrl, @publishedDate,
                @severityScore, @severityLevel, @affectedProducts, @vendorId, @rawData, FALSE
            )";

        using var insertCommand = new MySqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@cveId", request.CveId ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@title", request.Title);
        insertCommand.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@source", request.Source);
        insertCommand.Parameters.AddWithValue("@sourceUrl", request.SourceUrl ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@publishedDate", request.PublishedDate.HasValue ? request.PublishedDate.Value.Date : (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@severityScore", request.SeverityScore ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@severityLevel", request.SeverityLevel);
        insertCommand.Parameters.AddWithValue("@affectedProducts", request.AffectedProducts ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@vendorId", request.VendorId ?? (object)DBNull.Value);
        insertCommand.Parameters.AddWithValue("@rawData", rawDataJson ?? (object)DBNull.Value);

        await insertCommand.ExecuteNonQueryAsync();
        var vulnerabilityId = (int)insertCommand.LastInsertedId;

        await connection.CloseAsync();

        return Results.Ok(new VulnerabilityIngestResponse
        {
            Success = true,
            Message = "Vulnerability ingested successfully",
            VulnerabilityId = vulnerabilityId,
            IsDuplicate = false
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new VulnerabilityIngestResponse
        {
            Success = false,
            Message = $"Error ingesting vulnerability: {ex.Message}"
        });
    }
})
.WithName("IngestVulnerability")
.WithOpenApi();

// AI Rate vulnerability endpoint
app.MapPost("/api/vulnerabilities/rate", async (AIVulnerabilityRatingRequest request, DatabaseService dbService) =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get vulnerability details
        var vulnQuery = @"
            SELECT v.id, v.title, v.description, v.affected_products, v.vendor_id, v.severity_level,
                   ven.name as vendor_name
            FROM Vulnerabilities v
            LEFT JOIN Vendors ven ON v.vendor_id = ven.id
            WHERE v.id = @vulnerabilityId";
        
        using var vulnCommand = new MySqlCommand(vulnQuery, connection);
        vulnCommand.Parameters.AddWithValue("@vulnerabilityId", request.VulnerabilityId);
        using var vulnReader = await vulnCommand.ExecuteReaderAsync();

        if (!await vulnReader.ReadAsync())
        {
            await vulnReader.CloseAsync();
            await connection.CloseAsync();
            return Results.NotFound(new AIVulnerabilityRatingResponse
            {
                Success = false,
                Message = "Vulnerability not found"
            });
        }

        var vulnTitle = vulnReader.GetString("title");
        var vulnDescription = vulnReader.IsDBNull(vulnReader.GetOrdinal("description")) ? "" : vulnReader.GetString("description");
        var affectedProducts = vulnReader.IsDBNull(vulnReader.GetOrdinal("affected_products")) ? "" : vulnReader.GetString("affected_products");
        var vendorName = vulnReader.IsDBNull(vulnReader.GetOrdinal("vendor_name")) ? "" : vulnReader.GetString("vendor_name");
        var severityLevel = vulnReader.GetString("severity_level");
        await vulnReader.CloseAsync();

        // Get company's vendors
        var companyVendorsQuery = @"
            SELECT cv.vendors_json
            FROM CompanyVendors cv
            WHERE cv.company_id = @companyId AND cv.is_active = TRUE
            LIMIT 1";
        
        using var companyVendorsCommand = new MySqlCommand(companyVendorsQuery, connection);
        companyVendorsCommand.Parameters.AddWithValue("@companyId", request.CompanyId);
        var vendorsJson = await companyVendorsCommand.ExecuteScalarAsync() as string;

        // Get company details
        var companyQuery = "SELECT name, description FROM Companies WHERE id = @companyId";
        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@companyId", request.CompanyId);
        using var companyReader = await companyCommand.ExecuteReaderAsync();
        
        string companyName = "";
        string companyDescription = "";
        if (await companyReader.ReadAsync())
        {
            companyName = companyReader.GetString("name");
            companyDescription = companyReader.IsDBNull(companyReader.GetOrdinal("description")) ? "" : companyReader.GetString("description");
        }
        await companyReader.CloseAsync();

        // Parse company vendors
        var companyVendorIds = new List<int>();
        var companyVendorNames = new List<string>();
        if (!string.IsNullOrEmpty(vendorsJson))
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(vendorsJson);
            var vendorSelections = jsonDoc.RootElement.EnumerateArray();
            
            foreach (var vendorSelection in vendorSelections)
            {
                if (vendorSelection.TryGetProperty("vendor_id", out var vendorIdElement))
                {
                    var vendorId = vendorIdElement.GetInt32();
                    companyVendorIds.Add(vendorId);
                    
                    // Get vendor name
                    var vendorNameQuery = "SELECT name FROM Vendors WHERE id = @vendorId";
                    using var vendorNameCommand = new MySqlCommand(vendorNameQuery, connection);
                    vendorNameCommand.Parameters.AddWithValue("@vendorId", vendorId);
                    var vendorNameResult = await vendorNameCommand.ExecuteScalarAsync();
                    if (vendorNameResult != null)
                    {
                        companyVendorNames.Add(vendorNameResult.ToString() ?? "");
                    }
                }
            }
        }

        // Simple AI rating logic (can be enhanced with actual AI API)
        // For now, we'll do basic matching
        bool vendorMatch = false;
        bool useCaseMatch = false;
        int relevanceScore = 0;

        // Check vendor match
        if (!string.IsNullOrEmpty(vendorName) && companyVendorNames.Any(v => 
            vulnTitle.Contains(v, StringComparison.OrdinalIgnoreCase) ||
            vulnDescription.Contains(v, StringComparison.OrdinalIgnoreCase) ||
            affectedProducts.Contains(v, StringComparison.OrdinalIgnoreCase)))
        {
            vendorMatch = true;
            relevanceScore += 40;
        }

        // Check if vulnerability mentions company's vendors
        foreach (var companyVendorName in companyVendorNames)
        {
            if (vulnTitle.Contains(companyVendorName, StringComparison.OrdinalIgnoreCase) ||
                vulnDescription.Contains(companyVendorName, StringComparison.OrdinalIgnoreCase) ||
                affectedProducts.Contains(companyVendorName, StringComparison.OrdinalIgnoreCase))
            {
                vendorMatch = true;
                relevanceScore += 30;
                break;
            }
        }

        // Check use case match (basic keyword matching)
        if (!string.IsNullOrEmpty(companyDescription))
        {
            var companyKeywords = companyDescription.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var vulnText = (vulnTitle + " " + vulnDescription + " " + affectedProducts).ToLower();
            
            var matchingKeywords = companyKeywords.Count(k => vulnText.Contains(k));
            if (matchingKeywords > 0)
            {
                useCaseMatch = true;
                relevanceScore += Math.Min(30, matchingKeywords * 5);
            }
        }

        // Adjust score based on severity
        if (severityLevel == "Critical") relevanceScore += 20;
        else if (severityLevel == "High") relevanceScore += 15;
        else if (severityLevel == "Medium") relevanceScore += 10;
        else if (severityLevel == "Low") relevanceScore += 5;

        // Cap at 100
        relevanceScore = Math.Min(100, relevanceScore);

        bool isRelevant = relevanceScore >= 50; // Threshold for relevance

        var aiReasoning = $"Vendor Match: {vendorMatch}, Use Case Match: {useCaseMatch}, Severity: {severityLevel}. " +
                         $"Company uses: {string.Join(", ", companyVendorNames)}. " +
                         $"Relevance Score: {relevanceScore}/100";

        // Save rating to database
        var insertRatingQuery = @"
            INSERT INTO VulnerabilityRatings (
                vulnerability_id, company_id, relevance_score, ai_reasoning,
                is_relevant, vendor_match, use_case_match
            )
            VALUES (
                @vulnerabilityId, @companyId, @relevanceScore, @aiReasoning,
                @isRelevant, @vendorMatch, @useCaseMatch
            )
            ON DUPLICATE KEY UPDATE
                relevance_score = @relevanceScore,
                ai_reasoning = @aiReasoning,
                is_relevant = @isRelevant,
                vendor_match = @vendorMatch,
                use_case_match = @useCaseMatch,
                rated_at = NOW()";

        using var insertRatingCommand = new MySqlCommand(insertRatingQuery, connection);
        insertRatingCommand.Parameters.AddWithValue("@vulnerabilityId", request.VulnerabilityId);
        insertRatingCommand.Parameters.AddWithValue("@companyId", request.CompanyId);
        insertRatingCommand.Parameters.AddWithValue("@relevanceScore", relevanceScore);
        insertRatingCommand.Parameters.AddWithValue("@aiReasoning", aiReasoning);
        insertRatingCommand.Parameters.AddWithValue("@isRelevant", isRelevant);
        insertRatingCommand.Parameters.AddWithValue("@vendorMatch", vendorMatch);
        insertRatingCommand.Parameters.AddWithValue("@useCaseMatch", useCaseMatch);
        await insertRatingCommand.ExecuteNonQueryAsync();

        await connection.CloseAsync();

        return Results.Ok(new AIVulnerabilityRatingResponse
        {
            Success = true,
            Message = "Vulnerability rated successfully",
            RelevanceScore = relevanceScore,
            AiReasoning = aiReasoning,
            IsRelevant = isRelevant,
            VendorMatch = vendorMatch,
            UseCaseMatch = useCaseMatch
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new AIVulnerabilityRatingResponse
        {
            Success = false,
            Message = $"Error rating vulnerability: {ex.Message}"
        });
    }
})
.WithName("RateVulnerability")
.WithOpenApi();

// Download vulnerabilities for all vendors from NVD
app.MapPost("/api/vulnerabilities/download-all", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get all vendors
        var vendorsQuery = "SELECT id, name FROM Vendors";
        using var vendorsCommand = new MySqlCommand(vendorsQuery, connection);
        using var vendorsReader = await vendorsCommand.ExecuteReaderAsync();
        
        var vendors = new List<(int Id, string Name)>();
        while (await vendorsReader.ReadAsync())
        {
            vendors.Add((vendorsReader.GetInt32("id"), vendorsReader.GetString("name")));
        }
        await vendorsReader.CloseAsync();

        if (vendors.Count == 0)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Message = "No vendors found", TotalDownloaded = 0 });
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "VulnRadar/1.0");
        
        var totalDownloaded = 0;
        var totalDuplicates = 0;
        var vendorResults = new List<object>();

        foreach (var vendor in vendors)
        {
            try
            {
                // NVD API search by keyword (vendor name)
                // Using the NVD REST API v2.0
                var nvdUrl = $"https://services.nvd.nist.gov/rest/json/cves/2.0?keywordSearch={Uri.EscapeDataString(vendor.Name)}&resultsPerPage=20";
                
                Console.WriteLine($"Downloading vulnerabilities for {vendor.Name}...");
                var response = await httpClient.GetAsync(nvdUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data for {vendor.Name}: {response.StatusCode}");
                    vendorResults.Add(new { Vendor = vendor.Name, Status = "Failed", Count = 0, Error = response.StatusCode.ToString() });
                    continue;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var nvdData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonContent);

                if (!nvdData.TryGetProperty("vulnerabilities", out var vulnerabilitiesArray))
                {
                    vendorResults.Add(new { Vendor = vendor.Name, Status = "No vulnerabilities found", Count = 0 });
                    continue;
                }

                var downloaded = 0;
                var duplicates = 0;

                foreach (var vulnElement in vulnerabilitiesArray.EnumerateArray())
                {
                    try
                    {
                        var cve = vulnElement.GetProperty("cve");
                        var cveId = cve.GetProperty("id").GetString();
                        var descriptions = cve.GetProperty("descriptions");
                        var descriptionElement = descriptions.EnumerateArray()
                            .FirstOrDefault(d => d.TryGetProperty("lang", out var lang) && lang.GetString() == "en");
                        string? description = null;
                        if (descriptionElement.ValueKind != JsonValueKind.Undefined)
                        {
                            description = descriptionElement.GetProperty("value").GetString();
                        }

                        // Get CVSS score if available
                        decimal? severityScore = null;
                        string severityLevel = "Unknown";
                        
                        if (cve.TryGetProperty("metrics", out var metrics))
                        {
                            if (metrics.TryGetProperty("cvssMetricV31", out var cvss31))
                            {
                                var cvssData = cvss31.EnumerateArray().FirstOrDefault();
                                if (cvssData.ValueKind != JsonValueKind.Undefined)
                                {
                                    severityScore = cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDecimal();
                                    var baseSeverity = cvssData.GetProperty("cvssData").GetProperty("baseSeverity").GetString();
                                    severityLevel = baseSeverity ?? "Unknown";
                                }
                            }
                            else if (metrics.TryGetProperty("cvssMetricV30", out var cvss30))
                            {
                                var cvssData = cvss30.EnumerateArray().FirstOrDefault();
                                if (cvssData.ValueKind != JsonValueKind.Undefined)
                                {
                                    severityScore = cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDecimal();
                                    var baseSeverity = cvssData.GetProperty("cvssData").GetProperty("baseSeverity").GetString();
                                    severityLevel = baseSeverity ?? "Unknown";
                                }
                            }
                            else if (metrics.TryGetProperty("cvssMetricV2", out var cvss2))
                            {
                                var cvssData = cvss2.EnumerateArray().FirstOrDefault();
                                if (cvssData.ValueKind != JsonValueKind.Undefined)
                                {
                                    severityScore = (decimal)cvssData.GetProperty("cvssData").GetProperty("baseScore").GetDouble();
                                    var severity = cvssData.GetProperty("baseSeverity").GetString();
                                    severityLevel = severity ?? "Unknown";
                                }
                            }
                        }

                        // Get published date
                        DateTime? publishedDate = null;
                        if (cve.TryGetProperty("published", out var published))
                        {
                            if (DateTime.TryParse(published.GetString(), out var pubDate))
                            {
                                publishedDate = pubDate;
                            }
                        }

                        // Get affected products from configurations
                        var affectedProducts = new List<string>();
                        if (cve.TryGetProperty("configurations", out var configs))
                        {
                            foreach (var config in configs.EnumerateArray())
                            {
                                if (config.TryGetProperty("nodes", out var nodes))
                                {
                                    foreach (var node in nodes.EnumerateArray())
                                    {
                                        if (node.TryGetProperty("cpeMatch", out var cpeMatches))
                                        {
                                            foreach (var cpe in cpeMatches.EnumerateArray())
                                            {
                                                if (cpe.TryGetProperty("criteria", out var criteria))
                                                {
                                                    var criteriaStr = criteria.GetString();
                                                    if (!string.IsNullOrEmpty(criteriaStr))
                                                    {
                                                        affectedProducts.Add(criteriaStr);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Check for duplicates
                        var checkCveQuery = "SELECT id FROM Vulnerabilities WHERE cve_id = @cveId LIMIT 1";
                        using var checkCveCommand = new MySqlCommand(checkCveQuery, connection);
                        checkCveCommand.Parameters.AddWithValue("@cveId", cveId);
                        var existingId = await checkCveCommand.ExecuteScalarAsync();

                        if (existingId != null)
                        {
                            duplicates++;
                            continue;
                        }

                        // Insert vulnerability
                        var title = $"{cveId}: {(description != null && description.Length > 0 ? description.Substring(0, Math.Min(500, description.Length)) : "No description")}";
                        var insertQuery = @"
                            INSERT INTO Vulnerabilities (
                                cve_id, title, description, source, source_url, published_date,
                                severity_score, severity_level, affected_products, vendor_id, raw_data, is_duplicate
                            )
                            VALUES (
                                @cveId, @title, @description, @source, @sourceUrl, @publishedDate,
                                @severityScore, @severityLevel, @affectedProducts, @vendorId, @rawData, FALSE
                            )";

                        using var insertCommand = new MySqlCommand(insertQuery, connection);
                        insertCommand.Parameters.AddWithValue("@cveId", cveId);
                        insertCommand.Parameters.AddWithValue("@title", title);
                        insertCommand.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@source", "NVD");
                        insertCommand.Parameters.AddWithValue("@sourceUrl", $"https://nvd.nist.gov/vuln/detail/{cveId}");
                        insertCommand.Parameters.AddWithValue("@publishedDate", publishedDate.HasValue ? publishedDate.Value.Date : (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@severityScore", severityScore ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@severityLevel", severityLevel);
                        insertCommand.Parameters.AddWithValue("@affectedProducts", affectedProducts.Count > 0 ? string.Join(", ", affectedProducts) : (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@vendorId", vendor.Id);
                        insertCommand.Parameters.AddWithValue("@rawData", jsonContent);

                        await insertCommand.ExecuteNonQueryAsync();
                        downloaded++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing vulnerability for {vendor.Name}: {ex.Message}");
                        continue;
                    }
                }

                totalDownloaded += downloaded;
                totalDuplicates += duplicates;
                vendorResults.Add(new { Vendor = vendor.Name, Status = "Success", Downloaded = downloaded, Duplicates = duplicates });

                // Rate limit: wait 6 seconds between requests (NVD allows 5 requests per 30 seconds)
                await Task.Delay(6000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading vulnerabilities for {vendor.Name}: {ex.Message}");
                vendorResults.Add(new { Vendor = vendor.Name, Status = "Error", Error = ex.Message });
            }
        }

        await connection.CloseAsync();

        return Results.Ok(new
        {
            Success = true,
            Message = $"Downloaded {totalDownloaded} vulnerabilities ({totalDuplicates} duplicates skipped)",
            TotalDownloaded = totalDownloaded,
            TotalDuplicates = totalDuplicates,
            VendorResults = vendorResults
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Success = false,
            Message = $"Error downloading vulnerabilities: {ex.Message}"
        });
    }
})
.WithName("DownloadAllVulnerabilities")
.WithOpenApi();

// Get all tasks endpoint
app.MapGet("/api/tasks", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, vulnerability_id, company_id, assigned_by_user_id, assigned_to_user_id, 
                     priority, status, notes, created_at, updated_at, resolved_at 
                     FROM Tasks 
                     ORDER BY created_at DESC";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var tasks = new List<object>();
        while (await reader.ReadAsync())
        {
            tasks.Add(new
            {
                Id = reader.GetInt32("id"),
                VulnerabilityId = reader.GetInt32("vulnerability_id"),
                CompanyId = reader.GetInt32("company_id"),
                AssignedByUserId = reader.GetInt32("assigned_by_user_id"),
                AssignedToUserId = reader.GetInt32("assigned_to_user_id"),
                Priority = reader.GetString("priority"),
                Status = reader.GetString("status"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at"),
                ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime("resolved_at").ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = tasks.Count, Tasks = tasks });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllTasks")
.WithOpenApi();

// Get all vulnerability ratings endpoint
app.MapGet("/api/vulnerability-ratings", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, vulnerability_id, company_id, relevance_score, ai_reasoning, 
                     is_relevant, vendor_match, use_case_match, rated_at 
                     FROM VulnerabilityRatings 
                     ORDER BY rated_at DESC";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var ratings = new List<object>();
        while (await reader.ReadAsync())
        {
            ratings.Add(new
            {
                Id = reader.GetInt32("id"),
                VulnerabilityId = reader.GetInt32("vulnerability_id"),
                CompanyId = reader.GetInt32("company_id"),
                RelevanceScore = reader.GetInt32("relevance_score"),
                AiReasoning = reader.IsDBNull(reader.GetOrdinal("ai_reasoning")) ? null : reader.GetString("ai_reasoning"),
                IsRelevant = reader.GetBoolean("is_relevant"),
                VendorMatch = reader.GetBoolean("vendor_match"),
                UseCaseMatch = reader.GetBoolean("use_case_match"),
                RatedAt = reader.GetDateTime("rated_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = ratings.Count, Ratings = ratings });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllVulnerabilityRatings")
.WithOpenApi();

// Get all vulnerability resolutions endpoint
app.MapGet("/api/vulnerability-resolutions", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, task_id, vulnerability_id, company_id, owner_user_id, 
                     resolution_status, resolution_notes, patch_applied_date, verified_date, 
                     created_at, updated_at 
                     FROM VulnerabilityResolutions 
                     ORDER BY created_at DESC";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var resolutions = new List<object>();
        while (await reader.ReadAsync())
        {
            resolutions.Add(new
            {
                Id = reader.GetInt32("id"),
                TaskId = reader.IsDBNull(reader.GetOrdinal("task_id")) ? (int?)null : reader.GetInt32("task_id"),
                VulnerabilityId = reader.GetInt32("vulnerability_id"),
                CompanyId = reader.GetInt32("company_id"),
                OwnerUserId = reader.GetInt32("owner_user_id"),
                ResolutionStatus = reader.GetString("resolution_status"),
                ResolutionNotes = reader.IsDBNull(reader.GetOrdinal("resolution_notes")) ? null : reader.GetString("resolution_notes"),
                PatchAppliedDate = reader.IsDBNull(reader.GetOrdinal("patch_applied_date")) ? null : reader.GetDateTime("patch_applied_date").ToString("yyyy-MM-dd HH:mm:ss"),
                VerifiedDate = reader.IsDBNull(reader.GetOrdinal("verified_date")) ? null : reader.GetDateTime("verified_date").ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = resolutions.Count, Resolutions = resolutions });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllVulnerabilityResolutions")
.WithOpenApi();

// Get all company vendors (junction table) endpoint
app.MapGet("/api/company-vendors", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, company_id, vendor_id, use_case_description, is_active, created_at 
                     FROM CompanyVendors 
                     ORDER BY created_at DESC";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var companyVendors = new List<object>();
        while (await reader.ReadAsync())
        {
            companyVendors.Add(new
            {
                Id = reader.GetInt32("id"),
                CompanyId = reader.GetInt32("company_id"),
                VendorId = reader.GetInt32("vendor_id"),
                UseCaseDescription = reader.IsDBNull(reader.GetOrdinal("use_case_description")) ? null : reader.GetString("use_case_description"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = companyVendors.Count, CompanyVendors = companyVendors });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllCompanyVendors")
.WithOpenApi();

// Get all user companies (junction table) endpoint
app.MapGet("/api/user-companies", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, user_id, company_id, is_primary, created_at 
                     FROM UserCompanies 
                     ORDER BY created_at DESC";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var userCompanies = new List<object>();
        while (await reader.ReadAsync())
        {
            userCompanies.Add(new
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                CompanyId = reader.GetInt32("company_id"),
                IsPrimary = reader.GetBoolean("is_primary"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = userCompanies.Count, UserCompanies = userCompanies });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllUserCompanies")
.WithOpenApi();

// Get all audit logs endpoint
app.MapGet("/api/audit-logs", async () =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT id, user_id, action_type, entity_type, entity_id, details, ip_address, created_at 
                     FROM AuditLogs 
                     ORDER BY created_at DESC 
                     LIMIT 100";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        var auditLogs = new List<object>();
        while (await reader.ReadAsync())
        {
            auditLogs.Add(new
            {
                Id = reader.GetInt32("id"),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? (int?)null : reader.GetInt32("user_id"),
                ActionType = reader.GetString("action_type"),
                EntityType = reader.IsDBNull(reader.GetOrdinal("entity_type")) ? null : reader.GetString("entity_type"),
                EntityId = reader.IsDBNull(reader.GetOrdinal("entity_id")) ? (int?)null : reader.GetInt32("entity_id"),
                Details = reader.IsDBNull(reader.GetOrdinal("details")) ? null : reader.GetString("details"),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("ip_address")) ? null : reader.GetString("ip_address"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Count = auditLogs.Count, AuditLogs = auditLogs });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetAllAuditLogs")
.WithOpenApi();

app.Run();
