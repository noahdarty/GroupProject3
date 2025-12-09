using MyProject.API.Services;
using MyProject.API.Data;
using MyProject.API.Models;
using MySqlConnector;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Parse Heroku DATABASE_URL if present, otherwise use appsettings
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse mysql://user:password@host:port/database format
    var uri = new Uri(databaseUrl);
    var connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};User={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SslMode=Required;";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}
// If no DATABASE_URL, check for direct connection string env var (for manual Heroku config)
else
{
    // Try double underscore first (correct format), then single underscore (common mistake)
    var directConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
        ?? Environment.GetEnvironmentVariable("ConnectionStrings_DefaultConnection");
    if (!string.IsNullOrEmpty(directConnectionString))
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = directConnectionString;
    }
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "VulnRadar API", 
        Version = "v1",
        Description = "Vulnerability Radar API"
    });
    // Discover all endpoints, even without .WithOpenApi()
    c.CustomSchemaIds(type => type.FullName);
});

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

// Configure port for Heroku (uses PORT env var, defaults to 5000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowFrontend");

// Enable Swagger in all environments (including Heroku)
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VulnRadar API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

// Root endpoint to test if app is running
app.MapGet("/", () => Results.Ok(new { 
    Message = "VulnRadar API is running", 
    Swagger = "/swagger",
    SwaggerJson = "/swagger/v1/swagger.json",
    Timestamp = DateTime.UtcNow 
}))
.WithName("Root")
.WithOpenApi();

// Test endpoint to verify Swagger JSON
app.MapGet("/swagger-test", () => Results.Redirect("/swagger/v1/swagger.json"))
.WithName("SwaggerTest")
.WithOpenApi();

// Don't use HTTPS redirection on Heroku - Heroku handles HTTPS at the load balancer
// if (!app.Environment.IsDevelopment())
// {
// app.UseHttpsRedirection();
// }

// Helper function to determine TLP rating based on realistic factors
// TLP is about information sensitivity and distribution, NOT severity
// Uses a hash of CVE ID + source to ensure independence from severity
static string DetermineTlpRating(string? source, string? cveId, DateTime? publishedDate)
{
    // If no source or CVE ID, it's likely internal/private information - RED
    if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(cveId))
    {
        return "RED";
    }

    // Use a hash of CVE ID to create variation independent of severity
    // This ensures TLP and severity are truly independent
    var hashInput = $"{cveId}_{source}";
    var hash = hashInput.GetHashCode();
    var hashMod = Math.Abs(hash % 100); // 0-99

    // If source is NVD (National Vulnerability Database), it's publicly available
    if (source.Equals("NVD", StringComparison.OrdinalIgnoreCase))
    {
        if (publishedDate.HasValue)
        {
            var daysSincePublished = (DateTime.Now - publishedDate.Value).TotalDays;
            
            // Very recent (within 7 days) - mix of AMBER and RED based on hash
            if (daysSincePublished <= 7)
            {
                // 60% AMBER, 40% RED (sensitive new disclosures)
                return hashMod < 60 ? "AMBER" : "RED";
            }
            // Recent (within 30 days) - mix of AMBER and GREEN
            else if (daysSincePublished <= 30)
            {
                // 70% AMBER, 30% GREEN
                return hashMod < 70 ? "AMBER" : "GREEN";
            }
            // Older but still within 90 days - mix of GREEN and AMBER
            else if (daysSincePublished <= 90)
            {
                // 80% GREEN, 20% AMBER
                return hashMod < 80 ? "GREEN" : "AMBER";
            }
        }
        
        // Older NVD entries - mostly GREEN but some variation
        // 85% GREEN, 10% AMBER, 5% RED (some may still be sensitive)
        if (hashMod < 85) return "GREEN";
        if (hashMod < 95) return "AMBER";
        return "RED";
    }

    // If source is CVE or has CVE ID but not from NVD - varied distribution
    if (!string.IsNullOrEmpty(cveId) && cveId.StartsWith("CVE-", StringComparison.OrdinalIgnoreCase))
    {
        if (publishedDate.HasValue)
        {
            var daysSincePublished = (DateTime.Now - publishedDate.Value).TotalDays;
            if (daysSincePublished <= 30)
            {
                // Recent non-NVD CVEs - 50% AMBER, 30% RED, 20% GREEN
                if (hashMod < 50) return "AMBER";
                if (hashMod < 80) return "RED";
                return "GREEN";
            }
        }
        // Older non-NVD CVEs - 70% GREEN, 20% AMBER, 10% RED
        if (hashMod < 70) return "GREEN";
        if (hashMod < 90) return "AMBER";
        return "RED";
    }

    // Internal/private sources without CVE - mostly RED but some variation
    // 80% RED, 15% AMBER, 5% GREEN
    if (hashMod < 80) return "RED";
    if (hashMod < 95) return "AMBER";
    return "GREEN";
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
        
        // Log role and companyId for debugging
        Console.WriteLine($"VerifyToken - Email: {firebaseUser.Email}, Role: {request.Role}, CompanyId: {request.CompanyId}");
        
        // Get or create user in database
        var dbUser = await dbService.GetUserByFirebaseUidAsync(firebaseUser.Uid);
        
        if (dbUser == null)
        {
            // Create new user in database with role and company if provided (during signup)
            Console.WriteLine($"Creating new user in database - Role: {request.Role}, CompanyId: {request.CompanyId}");
            await dbService.CreateOrUpdateUserFromFirebaseAsync(
                firebaseUser.Uid,
                firebaseUser.Email,
                firebaseUser.DisplayName,
                request.Role,
                request.CompanyId
            );
            
            // Get the newly created user
            dbUser = await dbService.GetUserByFirebaseUidAsync(firebaseUser.Uid);
            Console.WriteLine($"User created successfully - Email: {dbUser?.Email}, Role: {dbUser?.Role}, Company: {dbUser?.CompanyName}");
        }
        else
        {
            Console.WriteLine($"User already exists in database - Email: {dbUser.Email}, Role: {dbUser.Role}");
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
                     WHERE (description NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR description IS NULL)
                       AND (description NOT LIKE '%Rejected reason:%' OR description IS NULL)
                       AND (title NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR title IS NULL)
                       AND severity_level != 'Unknown'
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
.WithName("GetVulnerabilities")
.WithOpenApi();

// Get single vulnerability by ID endpoint
app.MapGet("/api/vulnerabilities/{id}", async (int id) =>
{
    try
    {
        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();
        
        var query = @"SELECT v.id, v.cve_id, v.title, v.description, v.source, v.source_url, 
                     v.published_date, v.severity_score, v.severity_level, v.affected_products, 
                     v.vendor_id, ven.name as vendor_name, v.created_at, v.updated_at
                     FROM Vulnerabilities v
                     LEFT JOIN Vendors ven ON v.vendor_id = ven.id
                     WHERE v.id = @id
                       AND (v.description NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.description IS NULL)
                       AND (v.description NOT LIKE '%Rejected reason:%' OR v.description IS NULL)
                       AND (v.title NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.title IS NULL)";
        
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = await command.ExecuteReaderAsync();
        
        if (!await reader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.NotFound(new { Success = false, Message = "Vulnerability not found" });
        }

        var vulnerability = new
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
            VendorName = reader.IsDBNull(reader.GetOrdinal("vendor_name")) ? null : reader.GetString("vendor_name"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Vulnerability = vulnerability });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetVulnerabilityById")
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

        // Determine TLP rating based on realistic factors (source, CVE status, published date)
        // TLP is about information sensitivity, NOT severity
        string tlpRating = DetermineTlpRating(request.Source, request.CveId, request.PublishedDate);

        var insertQuery = @"
            INSERT INTO Vulnerabilities (
                cve_id, title, description, source, source_url, published_date,
                severity_score, severity_level, tlp_rating, affected_products, vendor_id, raw_data, is_duplicate
            )
            VALUES (
                @cveId, @title, @description, @source, @sourceUrl, @publishedDate,
                @severityScore, @severityLevel, @tlpRating, @affectedProducts, @vendorId, @rawData, FALSE
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
        insertCommand.Parameters.AddWithValue("@tlpRating", tlpRating);
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
                        
                        // Check if CVE is rejected/invalid - skip these
                        if (cve.TryGetProperty("vulnStatus", out var vulnStatus))
                        {
                            var status = vulnStatus.GetString();
                            if (status == "Rejected" || status == "REJECTED")
                            {
                                // Skip rejected CVEs
                                continue;
                            }
                        }
                        
                        // Also check description for rejection keywords
                        var descriptions = cve.GetProperty("descriptions");
                        var descriptionElement = descriptions.EnumerateArray()
                            .FirstOrDefault(d => d.TryGetProperty("lang", out var lang) && lang.GetString() == "en");
                        string? description = null;
                        if (descriptionElement.ValueKind != JsonValueKind.Undefined)
                        {
                            description = descriptionElement.GetProperty("value").GetString();
                            
                            // Skip if description indicates rejection
                            if (!string.IsNullOrEmpty(description) && 
                                (description.Contains("DO NOT USE THIS CANDIDATE NUMBER", StringComparison.OrdinalIgnoreCase) ||
                                 description.Contains("Rejected reason:", StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }
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

                        // Determine TLP rating based on realistic factors (source, CVE status, published date)
                        // TLP is about information sensitivity, NOT severity
                        string tlpRating = DetermineTlpRating("NVD", cveId, publishedDate);

                        // Insert vulnerability
                        var title = $"{cveId}: {(description != null && description.Length > 0 ? description.Substring(0, Math.Min(500, description.Length)) : "No description")}";
                        var insertQuery = @"
                            INSERT INTO Vulnerabilities (
                                cve_id, title, description, source, source_url, published_date,
                                severity_score, severity_level, tlp_rating, affected_products, vendor_id, raw_data, is_duplicate
                            )
                            VALUES (
                                @cveId, @title, @description, @source, @sourceUrl, @publishedDate,
                                @severityScore, @severityLevel, @tlpRating, @affectedProducts, @vendorId, @rawData, FALSE
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
                        insertCommand.Parameters.AddWithValue("@tlpRating", tlpRating);
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

// Get tasks endpoint (filtered by user role)
app.MapGet("/api/tasks", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
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
        
        // Get user ID and role
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        if (!await userIdReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Tasks = new List<object>() });
        }

        var userId = userIdReader.GetInt32("id");
        var userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        await userIdReader.CloseAsync();

        // Get user's company
        var companyQuery = "SELECT company_id FROM UserCompanies WHERE user_id = @userId LIMIT 1";
        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", userId);
        var companyId = await companyCommand.ExecuteScalarAsync();

        if (companyId == null)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Tasks = new List<object>() });
        }

        // Build query based on role
        string query;
        if (userRole?.ToLower() == "admin")
        {
            // Admins see all tasks for their company
            query = @"
                SELECT t.id, t.vulnerability_id, t.company_id, t.assigned_by_user_id, t.assigned_to_user_id, 
                       t.priority, t.status, t.notes, t.created_at, t.updated_at, t.resolved_at,
                       v.cve_id, v.title, v.severity_level,
                       u1.email as assigned_by_email, u2.email as assigned_to_email
                FROM Tasks t
                INNER JOIN Vulnerabilities v ON t.vulnerability_id = v.id
                LEFT JOIN Users u1 ON t.assigned_by_user_id = u1.id
                LEFT JOIN Users u2 ON t.assigned_to_user_id = u2.id
                WHERE t.company_id = @companyId
                ORDER BY t.created_at DESC";
        }
        else
        {
            // Employees/Managers see only their assigned tasks
            query = @"
                SELECT t.id, t.vulnerability_id, t.company_id, t.assigned_by_user_id, t.assigned_to_user_id, 
                       t.priority, t.status, t.notes, t.created_at, t.updated_at, t.resolved_at,
                       v.cve_id, v.title, v.severity_level,
                       u1.email as assigned_by_email, u2.email as assigned_to_email
                FROM Tasks t
                INNER JOIN Vulnerabilities v ON t.vulnerability_id = v.id
                LEFT JOIN Users u1 ON t.assigned_by_user_id = u1.id
                LEFT JOIN Users u2 ON t.assigned_to_user_id = u2.id
                WHERE t.assigned_to_user_id = @userId AND t.company_id = @companyId
                ORDER BY t.created_at DESC";
        }

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@companyId", companyId);
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
                ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : (DateTime?)reader.GetDateTime("resolved_at"),
                CveId = reader.GetString("cve_id"),
                Title = reader.GetString("title"),
                SeverityLevel = reader.GetString("severity_level"),
                AssignedByEmail = reader.IsDBNull(reader.GetOrdinal("assigned_by_email")) ? null : reader.GetString("assigned_by_email"),
                AssignedToEmail = reader.IsDBNull(reader.GetOrdinal("assigned_to_email")) ? null : reader.GetString("assigned_to_email")
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
.WithName("GetTasks")
.WithOpenApi();

// Assign vulnerability to user endpoint
app.MapPost("/api/tasks", async (HttpRequest request, AssignTaskRequest taskRequest, FirebaseService firebaseService, DatabaseService dbService) =>
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

        // Get user ID and role (must be admin)
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        if (!await userIdReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.Unauthorized();
        }

        var assignedByUserId = userIdReader.GetInt32("id");
        var userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        await userIdReader.CloseAsync();

        if (userRole?.ToLower() != "admin")
        {
            await connection.CloseAsync();
            return Results.Forbid();
        }

        // Get company ID from vulnerability
        var vulnQuery = "SELECT vendor_id FROM Vulnerabilities WHERE id = @vulnId LIMIT 1";
        using var vulnCommand = new MySqlCommand(vulnQuery, connection);
        vulnCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        var vendorId = await vulnCommand.ExecuteScalarAsync();

        if (vendorId == null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Vulnerability not found" });
        }

        // Get company ID from user
        var companyQuery = "SELECT company_id FROM UserCompanies WHERE user_id = @userId LIMIT 1";
        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", assignedByUserId);
        var companyId = await companyCommand.ExecuteScalarAsync();

        if (companyId == null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Company not found" });
        }

        // Check if task already exists
        var checkQuery = "SELECT id FROM Tasks WHERE vulnerability_id = @vulnId AND company_id = @companyId AND assigned_to_user_id = @assignedToUserId LIMIT 1";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        checkCommand.Parameters.AddWithValue("@companyId", companyId);
        checkCommand.Parameters.AddWithValue("@assignedToUserId", taskRequest.AssignedToUserId);
        var existingTask = await checkCommand.ExecuteScalarAsync();

        if (existingTask != null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Task already assigned to this user" });
        }

        // Insert task
        var insertQuery = @"
            INSERT INTO Tasks (vulnerability_id, company_id, assigned_by_user_id, assigned_to_user_id, priority, status, notes)
            VALUES (@vulnId, @companyId, @assignedByUserId, @assignedToUserId, @priority, 'pending', @notes)";
        
        using var insertCommand = new MySqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        insertCommand.Parameters.AddWithValue("@companyId", companyId);
        insertCommand.Parameters.AddWithValue("@assignedByUserId", assignedByUserId);
        insertCommand.Parameters.AddWithValue("@assignedToUserId", taskRequest.AssignedToUserId);
        insertCommand.Parameters.AddWithValue("@priority", taskRequest.Priority ?? "Medium");
        insertCommand.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(taskRequest.Notes) ? (object)DBNull.Value : taskRequest.Notes);

        await insertCommand.ExecuteNonQueryAsync();
        var taskId = (int)insertCommand.LastInsertedId;

        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Message = "Task assigned successfully", TaskId = taskId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("AssignTask")
.WithOpenApi();

// Claim vulnerability (self-assign) endpoint
app.MapPost("/api/tasks/claim", async (HttpRequest request, ClaimTaskRequest taskRequest, FirebaseService firebaseService, DatabaseService dbService) =>
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
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        if (!await userIdReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.Unauthorized();
        }

        var userId = userIdReader.GetInt32("id");
        var userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        await userIdReader.CloseAsync();

        // Only employees and managers can claim (not admins)
        if (userRole?.ToLower() == "admin")
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Admins should use the assign feature instead." });
        }

        // Get company ID
        var companyQuery = "SELECT company_id FROM UserCompanies WHERE user_id = @userId LIMIT 1";
        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", userId);
        var companyId = await companyCommand.ExecuteScalarAsync();

        if (companyId == null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Company not found" });
        }

        // Check if task already exists for this user (only active tasks, not closed)
        var checkUserQuery = "SELECT id FROM Tasks WHERE vulnerability_id = @vulnId AND company_id = @companyId AND assigned_to_user_id = @userId AND status != 'closed' LIMIT 1";
        using var checkUserCommand = new MySqlCommand(checkUserQuery, connection);
        checkUserCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        checkUserCommand.Parameters.AddWithValue("@companyId", companyId);
        checkUserCommand.Parameters.AddWithValue("@userId", userId);
        var existingUserTask = await checkUserCommand.ExecuteScalarAsync();

        if (existingUserTask != null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "You have already claimed this vulnerability. Check 'My Tasks' to track your progress." });
        }

        // Check if vulnerability is already assigned to someone else (only active tasks)
        var checkAssignedQuery = "SELECT id, assigned_to_user_id FROM Tasks WHERE vulnerability_id = @vulnId AND company_id = @companyId AND status != 'closed' LIMIT 1";
        using var checkAssignedCommand = new MySqlCommand(checkAssignedQuery, connection);
        checkAssignedCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        checkAssignedCommand.Parameters.AddWithValue("@companyId", companyId);
        var existingAssignedTask = await checkAssignedCommand.ExecuteScalarAsync();

        if (existingAssignedTask != null)
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "This vulnerability has already been assigned to another user." });
        }

        // Get vulnerability severity for priority
        var vulnQuery = "SELECT severity_level FROM Vulnerabilities WHERE id = @vulnId LIMIT 1";
        using var vulnCommand = new MySqlCommand(vulnQuery, connection);
        vulnCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        var severityLevel = await vulnCommand.ExecuteScalarAsync() as string;
        
        // Map severity to priority
        string priority = "Medium";
        if (severityLevel?.ToLower() == "critical") priority = "Critical";
        else if (severityLevel?.ToLower() == "high") priority = "High";
        else if (severityLevel?.ToLower() == "low") priority = "Low";

        // Insert self-assigned task (assigned_by = assigned_to = current user)
        var insertQuery = @"
            INSERT INTO Tasks (vulnerability_id, company_id, assigned_by_user_id, assigned_to_user_id, priority, status, notes)
            VALUES (@vulnId, @companyId, @userId, @userId, @priority, 'pending', @notes)";
        
        using var insertCommand = new MySqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@vulnId", taskRequest.VulnerabilityId);
        insertCommand.Parameters.AddWithValue("@companyId", companyId);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@priority", priority);
        insertCommand.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(taskRequest.Notes) ? (object)DBNull.Value : "Self-assigned: Starting work on this vulnerability.");

        await insertCommand.ExecuteNonQueryAsync();
        var taskId = (int)insertCommand.LastInsertedId;

        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Message = "Vulnerability claimed successfully", TaskId = taskId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("ClaimVulnerability")
.WithOpenApi();

// Update task status endpoint
app.MapPut("/api/tasks/{taskId}", async (HttpRequest request, int taskId, UpdateTaskRequest updateRequest, FirebaseService firebaseService, DatabaseService dbService) =>
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
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        if (!await userIdReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.Unauthorized();
        }

        var userId = userIdReader.GetInt32("id");
        var userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        await userIdReader.CloseAsync();
        
        // Validate status based on role - only prevent non-admins from closing
        if (userRole?.ToLower() != "admin" && updateRequest.Status?.ToLower() == "closed")
        {
            await connection.CloseAsync();
            return Results.BadRequest(new { Success = false, Message = "Only administrators can close tasks." });
        }

        // Check if task exists and user has permission
        var checkQuery = "SELECT assigned_to_user_id, company_id FROM Tasks WHERE id = @taskId LIMIT 1";
        using var checkCommand = new MySqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@taskId", taskId);
        using var checkReader = await checkCommand.ExecuteReaderAsync();
        
        if (!await checkReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.NotFound(new { Success = false, Message = "Task not found" });
        }

        var assignedToUserId = checkReader.GetInt32("assigned_to_user_id");
        var companyId = checkReader.GetInt32("company_id");
        await checkReader.CloseAsync();

        // Only assigned user or admin can update
        if (assignedToUserId != userId && userRole?.ToLower() != "admin")
        {
            await connection.CloseAsync();
            return Results.Forbid();
        }

        // Update task
        var resolvedAt = updateRequest.Status?.ToLower() == "resolved" || updateRequest.Status?.ToLower() == "closed" 
            ? DateTime.UtcNow 
            : (object)DBNull.Value;

        var updateQuery = @"
            UPDATE Tasks 
            SET status = @status, notes = @notes, resolved_at = @resolvedAt, updated_at = CURRENT_TIMESTAMP
            WHERE id = @taskId";
        
        using var updateCommand = new MySqlCommand(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@taskId", taskId);
        updateCommand.Parameters.AddWithValue("@status", updateRequest.Status ?? "pending");
        updateCommand.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(updateRequest.Notes) ? (object)DBNull.Value : updateRequest.Notes);
        updateCommand.Parameters.AddWithValue("@resolvedAt", resolvedAt);

        await updateCommand.ExecuteNonQueryAsync();

        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Message = "Task updated successfully" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("UpdateTask")
.WithOpenApi();

// Get users in company endpoint (for assignment dropdown)
app.MapGet("/api/companies/{companyId}/users", async (HttpRequest request, int companyId, FirebaseService firebaseService, DatabaseService dbService) =>
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

        // Get user role (must be admin)
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        if (!await userIdReader.ReadAsync())
        {
            await connection.CloseAsync();
            return Results.Unauthorized();
        }

        var userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        await userIdReader.CloseAsync();

        if (userRole?.ToLower() != "admin")
        {
            await connection.CloseAsync();
            return Results.Forbid();
        }

        // Get users in company (exclude admins - they don't need to be assigned tasks)
        var query = @"
            SELECT u.id, u.email, u.role, u.full_name
            FROM Users u
            INNER JOIN UserCompanies uc ON u.id = uc.user_id
            WHERE uc.company_id = @companyId
              AND (u.role IS NULL OR LOWER(u.role) != 'admin')
            ORDER BY u.email";
        
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@companyId", companyId);
        using var reader = await command.ExecuteReaderAsync();
        
        var users = new List<object>();
        while (await reader.ReadAsync())
        {
            var fullName = reader.IsDBNull(reader.GetOrdinal("full_name")) ? null : reader.GetString("full_name");
            // Split full_name into first and last name if it contains a space
            string? firstName = null;
            string? lastName = null;
            if (!string.IsNullOrEmpty(fullName))
            {
                var nameParts = fullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length > 0) firstName = nameParts[0];
                if (nameParts.Length > 1) lastName = nameParts[1];
            }
            
            users.Add(new
            {
                Id = reader.GetInt32("id"),
                Email = reader.GetString("email"),
                Role = reader.IsDBNull(reader.GetOrdinal("role")) ? "employee" : reader.GetString("role"),
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { Success = true, Users = users });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetCompanyUsers")
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

// Get vulnerabilities for user's company with role-based filtering
app.MapGet("/api/vulnerabilities/company", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
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

        // Get optional TLP filter from query string (for admins)
        var tlpFilterParam = request.Query["tlpRating"].ToString().ToUpper();
        bool hasTlpFilter = !string.IsNullOrEmpty(tlpFilterParam) && 
                           (tlpFilterParam == "RED" || tlpFilterParam == "AMBER" || tlpFilterParam == "GREEN");

        using var connection = new MySqlConnection(
            app.Configuration.GetConnectionString("DefaultConnection"));
        
        await connection.OpenAsync();

        // Get user ID and role
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        int userId = 0;
        string userRole = "";
        if (await userIdReader.ReadAsync())
        {
            userId = userIdReader.GetInt32("id");
            userRole = userIdReader.GetString("role").ToLower().Trim();
        }
        await userIdReader.CloseAsync();
        
        // Log role for debugging
        Console.WriteLine($"User {dbUser.Email} has role: '{userRole}' (userId: {userId})");

        if (userId == 0)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vulnerabilities = new List<object>() });
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
            return Results.Ok(new { Success = true, Vulnerabilities = new List<object>() });
        }

        // Get company's vendor IDs
        var vendorIdsQuery = @"
            SELECT DISTINCT cv.vendor_id
            FROM CompanyVendors cv
            WHERE cv.company_id = @companyId AND cv.is_active = TRUE";
        using var vendorIdsCommand = new MySqlCommand(vendorIdsQuery, connection);
        vendorIdsCommand.Parameters.AddWithValue("@companyId", companyId);
        using var vendorIdsReader = await vendorIdsCommand.ExecuteReaderAsync();
        
        var companyVendorIds = new List<int>();
        while (await vendorIdsReader.ReadAsync())
        {
            companyVendorIds.Add(vendorIdsReader.GetInt32("vendor_id"));
        }
        await vendorIdsReader.CloseAsync();

        if (companyVendorIds.Count == 0)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vulnerabilities = new List<object>(), Message = "No vendors selected for company" });
        }

        // Build query to get vulnerabilities for company's vendors
        // Filter out rejected/invalid CVEs and closed vulnerabilities
        // Include task assignment information
        // No relevance scoring - just show vendor-matched vulnerabilities
        // Filter by TLP rating based on user role
        string tlpFilter = "";
        if (userRole == "admin")
        {
            // Admins can see all TLP ratings, or filter by specific rating if provided
            if (hasTlpFilter)
            {
                tlpFilter = $"v.tlp_rating = '{tlpFilterParam}'";
            }
            else
            {
                // No filter - show all TLP ratings
                tlpFilter = "1=1"; // Always true condition
            }
        }
        else if (userRole == "manager")
        {
            // Managers see GREEN and AMBER (moderately sensitive and shareable)
            tlpFilter = "v.tlp_rating IN ('GREEN', 'AMBER')";
        }
        else
        {
            // Employees see GREEN (can share within community)
            tlpFilter = "v.tlp_rating = 'GREEN'";
        }

        // Build additional filter for assigned vulnerabilities
        // All users (including admins) should not see assigned vulnerabilities on the main vulnerabilities page
        // Exclude vulnerabilities that have an active task (not closed)
        string assignedFilter = "AND NOT EXISTS (SELECT 1 FROM Tasks t2 WHERE t2.vulnerability_id = v.id AND t2.company_id = @companyId AND t2.status != 'closed')";

        var vulnerabilitiesQuery = @"
            SELECT DISTINCT
                v.id, v.cve_id, v.title, v.description, v.source, v.source_url, 
                v.published_date, v.severity_score, v.severity_level, v.tlp_rating, v.affected_products,
                v.vendor_id, ven.name as vendor_name,
                t.id as task_id, t.status as task_status,
                u.email as assigned_to_email, u.full_name as assigned_to_name
            FROM Vulnerabilities v
            INNER JOIN Vendors ven ON v.vendor_id = ven.id
            LEFT JOIN Tasks t ON v.id = t.vulnerability_id AND t.company_id = @companyId AND t.status != 'closed'
            LEFT JOIN Users u ON t.assigned_to_user_id = u.id
            WHERE v.vendor_id IN (" + string.Join(",", companyVendorIds) + @")
              AND v.is_duplicate = FALSE
              AND (v.description NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.description IS NULL)
              AND (v.description NOT LIKE '%Rejected reason:%' OR v.description IS NULL)
              AND (v.title NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.title IS NULL)
              AND v.severity_level != 'Unknown'
              AND " + tlpFilter + @"
              " + assignedFilter + @"
            ORDER BY v.published_date DESC
            LIMIT 500";

        var vulnerabilities = new List<object>();
        using var vulnCommand = new MySqlCommand(vulnerabilitiesQuery, connection);
        vulnCommand.Parameters.AddWithValue("@companyId", companyId);
        using var vulnReader = await vulnCommand.ExecuteReaderAsync();

        while (await vulnReader.ReadAsync())
        {
            var severityLevel = vulnReader.GetString("severity_level");
            // TLP filtering is now done in SQL query, so all results here are already filtered correctly

            // Get task assignment info
            var taskIdOrdinal = vulnReader.GetOrdinal("task_id");
            var taskStatusOrdinal = vulnReader.GetOrdinal("task_status");
            var assignedToEmailOrdinal = vulnReader.GetOrdinal("assigned_to_email");
            var assignedToNameOrdinal = vulnReader.GetOrdinal("assigned_to_name");
            
            int? taskId = vulnReader.IsDBNull(taskIdOrdinal) ? null : vulnReader.GetInt32(taskIdOrdinal);
            string? taskStatus = vulnReader.IsDBNull(taskStatusOrdinal) ? null : vulnReader.GetString(taskStatusOrdinal);
            string? assignedToEmail = vulnReader.IsDBNull(assignedToEmailOrdinal) ? null : vulnReader.GetString(assignedToEmailOrdinal);
            string? assignedToName = vulnReader.IsDBNull(assignedToNameOrdinal) ? null : vulnReader.GetString(assignedToNameOrdinal);

            vulnerabilities.Add(new
            {
                Id = vulnReader.GetInt32("id"),
                CveId = vulnReader.IsDBNull(vulnReader.GetOrdinal("cve_id")) ? null : vulnReader.GetString("cve_id"),
                Title = vulnReader.GetString("title"),
                Description = vulnReader.IsDBNull(vulnReader.GetOrdinal("description")) ? null : vulnReader.GetString("description"),
                Source = vulnReader.GetString("source"),
                SourceUrl = vulnReader.IsDBNull(vulnReader.GetOrdinal("source_url")) ? null : vulnReader.GetString("source_url"),
                PublishedDate = vulnReader.IsDBNull(vulnReader.GetOrdinal("published_date")) ? null : vulnReader.GetDateTime("published_date").ToString("yyyy-MM-dd"),
                SeverityScore = vulnReader.IsDBNull(vulnReader.GetOrdinal("severity_score")) ? (decimal?)null : vulnReader.GetDecimal("severity_score"),
                SeverityLevel = vulnReader.GetString("severity_level"),
                TlpRating = vulnReader.IsDBNull(vulnReader.GetOrdinal("tlp_rating")) ? null : vulnReader.GetString("tlp_rating"),
                AffectedProducts = vulnReader.IsDBNull(vulnReader.GetOrdinal("affected_products")) ? null : vulnReader.GetString("affected_products"),
                VendorId = vulnReader.IsDBNull(vulnReader.GetOrdinal("vendor_id")) ? (int?)null : vulnReader.GetInt32("vendor_id"),
                VendorName = vulnReader.IsDBNull(vulnReader.GetOrdinal("vendor_name")) ? null : vulnReader.GetString("vendor_name"),
                TaskId = taskId,
                TaskStatus = taskStatus,
                AssignedToEmail = assignedToEmail,
                AssignedToName = assignedToName
            });
        }

        await connection.CloseAsync();
        
        return Results.Ok(new { 
            Success = true, 
            Count = vulnerabilities.Count, 
            Vulnerabilities = vulnerabilities,
            UserRole = userRole
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetCompanyVulnerabilities")
.WithOpenApi();

// Get completed vulnerabilities endpoint (admin only - for their company)
app.MapGet("/api/vulnerabilities/completed", async (HttpRequest request, FirebaseService firebaseService, DatabaseService dbService) =>
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

        // Get user ID and role
        var userIdQuery = "SELECT id, role FROM Users WHERE email = @email OR firebase_uid = @uid LIMIT 1";
        using var userIdCommand = new MySqlCommand(userIdQuery, connection);
        userIdCommand.Parameters.AddWithValue("@email", dbUser.Email);
        userIdCommand.Parameters.AddWithValue("@uid", dbUser.Uid);
        using var userIdReader = await userIdCommand.ExecuteReaderAsync();
        
        int userId = 0;
        string userRole = "";
        if (await userIdReader.ReadAsync())
        {
            userId = userIdReader.GetInt32("id");
            userRole = userIdReader.IsDBNull(userIdReader.GetOrdinal("role")) ? "employee" : userIdReader.GetString("role");
        }
        await userIdReader.CloseAsync();

        // Only admins can view completed vulnerabilities
        if (userRole?.ToLower() != "admin")
        {
            await connection.CloseAsync();
            return Results.Json(new { Success = false, Message = "Only administrators can view completed vulnerabilities." }, statusCode: 403);
        }

        // Get user's company
        var companyQuery = @"
            SELECT uc.company_id
            FROM UserCompanies uc
            WHERE uc.user_id = @userId
            LIMIT 1";
        using var companyCommand = new MySqlCommand(companyQuery, connection);
        companyCommand.Parameters.AddWithValue("@userId", userId);
        var companyIdObj = await companyCommand.ExecuteScalarAsync();

        if (companyIdObj == null)
        {
            await connection.CloseAsync();
            return Results.Ok(new { Success = true, Vulnerabilities = new List<object>(), Message = "No company associated with user" });
        }

        int companyId = Convert.ToInt32(companyIdObj);

        // Get completed (closed) vulnerabilities for the company
        var completedVulnerabilitiesQuery = @"
            SELECT 
                v.id, v.cve_id, v.title, v.description, v.source, v.source_url, 
                v.published_date, v.severity_score, v.severity_level, v.tlp_rating, v.affected_products,
                v.vendor_id, ven.name as vendor_name,
                t.id as task_id, t.status as task_status, t.company_id, t.resolved_at,
                u.email as assigned_to_email, u.full_name as assigned_to_name,
                c.name as company_name
            FROM Vulnerabilities v
            INNER JOIN Vendors ven ON v.vendor_id = ven.id
            INNER JOIN Tasks t ON v.id = t.vulnerability_id AND t.company_id = @companyId AND t.status = 'closed'
            LEFT JOIN Users u ON t.assigned_to_user_id = u.id
            LEFT JOIN Companies c ON t.company_id = c.id
            WHERE v.is_duplicate = FALSE
              AND (v.description NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.description IS NULL)
              AND (v.description NOT LIKE '%Rejected reason:%' OR v.description IS NULL)
              AND (v.title NOT LIKE '%DO NOT USE THIS CANDIDATE NUMBER%' OR v.title IS NULL)
              AND v.severity_level != 'Unknown'
            ORDER BY t.resolved_at DESC, v.published_date DESC
            LIMIT 1000";

        var vulnerabilities = new List<object>();
        using var vulnCommand = new MySqlCommand(completedVulnerabilitiesQuery, connection);
        vulnCommand.Parameters.AddWithValue("@companyId", companyId);
        using var vulnReader = await vulnCommand.ExecuteReaderAsync();

        while (await vulnReader.ReadAsync())
        {
            // Get task assignment info - check if columns exist and are not null
            int? taskId = null;
            string? taskStatus = null;
            string? assignedToEmail = null;
            string? assignedToName = null;
            string? companyName = null;
            DateTime? resolvedAt = null;
            
            try
            {
                var taskIdOrdinal = vulnReader.GetOrdinal("task_id");
                if (!vulnReader.IsDBNull(taskIdOrdinal))
                {
                    taskId = vulnReader.GetInt32(taskIdOrdinal);
                }
            }
            catch { }
            
            try
            {
                var taskStatusOrdinal = vulnReader.GetOrdinal("task_status");
                if (!vulnReader.IsDBNull(taskStatusOrdinal))
                {
                    taskStatus = vulnReader.GetString(taskStatusOrdinal);
                }
            }
            catch { }
            
            try
            {
                var assignedToEmailOrdinal = vulnReader.GetOrdinal("assigned_to_email");
                if (!vulnReader.IsDBNull(assignedToEmailOrdinal))
                {
                    assignedToEmail = vulnReader.GetString(assignedToEmailOrdinal);
                }
            }
            catch { }
            
            try
            {
                var assignedToNameOrdinal = vulnReader.GetOrdinal("assigned_to_name");
                if (!vulnReader.IsDBNull(assignedToNameOrdinal))
                {
                    assignedToName = vulnReader.GetString(assignedToNameOrdinal);
                }
            }
            catch { }
            
            try
            {
                var companyNameOrdinal = vulnReader.GetOrdinal("company_name");
                if (!vulnReader.IsDBNull(companyNameOrdinal))
                {
                    companyName = vulnReader.GetString(companyNameOrdinal);
                }
            }
            catch { }
            
            try
            {
                var resolvedAtOrdinal = vulnReader.GetOrdinal("resolved_at");
                if (!vulnReader.IsDBNull(resolvedAtOrdinal))
                {
                    resolvedAt = vulnReader.GetDateTime(resolvedAtOrdinal);
                }
            }
            catch { }

            vulnerabilities.Add(new
            {
                Id = vulnReader.GetInt32("id"),
                CveId = vulnReader.IsDBNull(vulnReader.GetOrdinal("cve_id")) ? null : vulnReader.GetString("cve_id"),
                Title = vulnReader.GetString("title"),
                Description = vulnReader.IsDBNull(vulnReader.GetOrdinal("description")) ? null : vulnReader.GetString("description"),
                Source = vulnReader.GetString("source"),
                SourceUrl = vulnReader.IsDBNull(vulnReader.GetOrdinal("source_url")) ? null : vulnReader.GetString("source_url"),
                PublishedDate = vulnReader.IsDBNull(vulnReader.GetOrdinal("published_date")) ? null : vulnReader.GetDateTime("published_date").ToString("yyyy-MM-dd"),
                SeverityScore = vulnReader.IsDBNull(vulnReader.GetOrdinal("severity_score")) ? (decimal?)null : vulnReader.GetDecimal("severity_score"),
                SeverityLevel = vulnReader.GetString("severity_level"),
                TlpRating = vulnReader.IsDBNull(vulnReader.GetOrdinal("tlp_rating")) ? null : vulnReader.GetString("tlp_rating"),
                AffectedProducts = vulnReader.IsDBNull(vulnReader.GetOrdinal("affected_products")) ? null : vulnReader.GetString("affected_products"),
                VendorId = vulnReader.IsDBNull(vulnReader.GetOrdinal("vendor_id")) ? (int?)null : vulnReader.GetInt32("vendor_id"),
                VendorName = vulnReader.IsDBNull(vulnReader.GetOrdinal("vendor_name")) ? null : vulnReader.GetString("vendor_name"),
                TaskId = taskId,
                TaskStatus = taskStatus,
                CompanyId = companyId, // Use the outer scope companyId
                CompanyName = companyName,
                AssignedToEmail = assignedToEmail,
                AssignedToName = assignedToName,
                ResolvedAt = resolvedAt?.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        
        await connection.CloseAsync();
        
        return Results.Ok(new { 
            Success = true, 
            Count = vulnerabilities.Count, 
            Vulnerabilities = vulnerabilities
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetAllVulnerabilities: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return Results.BadRequest(new { Success = false, Message = $"Error: {ex.Message}" });
    }
})
.WithName("GetCompletedVulnerabilities")
.WithOpenApi();

app.Run();
