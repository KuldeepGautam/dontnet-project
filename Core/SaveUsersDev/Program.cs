using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Dev-only endpoint: looks up RoleId by RoleName in M_Role, then inserts into M_Users
// with the password hashed the same way as AIM's AuthenticationService (BCrypt, work factor 12).
app.MapPost("/api/SaveUserName", async (SaveUserRequest req) =>
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    await using var roleCmd = new SqlCommand("SELECT RoleId FROM M_Role WHERE RoleName = @RoleName", conn);
    roleCmd.Parameters.AddWithValue("@RoleName", req.role_name);
    var roleId = await roleCmd.ExecuteScalarAsync();

    if (roleId is null)
    {
        return Results.BadRequest(new { error = "Role not found" });
    }

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.password, 12);

    await using var insertCmd = new SqlCommand(
        "INSERT INTO M_Users (UserName, Password, EmailAddress, RoleId) VALUES (@UserName, @Password, @EmailAddress, @RoleId)",
        conn);
    insertCmd.Parameters.AddWithValue("@UserName", req.username);
    insertCmd.Parameters.AddWithValue("@Password", passwordHash);
    insertCmd.Parameters.AddWithValue("@EmailAddress", req.email_address);
    insertCmd.Parameters.AddWithValue("@RoleId", roleId);
    await insertCmd.ExecuteNonQueryAsync();

    return Results.Ok(new { message = "User created" });
});

app.Run();

record SaveUserRequest(string username, string password, string email_address, string role_name);
