using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    
    services.AddControllers();
    services.AddDbContext<TodoContext>(opt =>
        opt.UseInMemoryDatabase("TodoList"));   
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    ConfigureAuthentication(services, configuration);
    // services.AddAuthorization();
    ConfigureAuthorization(services, configuration);
}

void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var keycloakSettings = configuration.GetSection("Authentication:Keycloak");
    var authority = keycloakSettings["Authority"];
    var audience = keycloakSettings["Audience"];
    var requireHttpsMetadata = bool.Parse(keycloakSettings["RequireHttpsMetadata"] ?? "false");

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; 
    })
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true
        };
    });
}

void ConfigureAuthorization(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthorization(opt=>
    {
        opt.AddPolicy("ClientRolePolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                try
                {
                    var resourceAccess = context.User.FindFirst("resource_access")?.Value;
                    if (resourceAccess != null)
                    {
                        var resourceAccessJson = JsonDocument.Parse(resourceAccess).RootElement;

                        if (resourceAccessJson.TryGetProperty("todo-client", out var todoClientProperty))
                        {
                            if (todoClientProperty.TryGetProperty("roles", out var roles))
                            {
                                return roles.EnumerateArray().Any(role => role.GetString() == "todo_role");
                            }
                        }
                    }
                }
                catch (Exception){
                    Console.WriteLine("user does not contain 'todo_role'");
                }
                return false;
            });
        });
    });
}

void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers().RequireAuthorization();
}