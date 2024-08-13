# Integrating Keycloak Authorization with ASP.NET Core

## Introduction

This tutorial provides a step-by-step guide to integrating Keycloak authorization with an ASP.NET Core application this README is derived from [here](https://github.com/Basemism/KeyCloak-ASP.NET-Core-API-Integration/blob/main/Integrating%20KeyCloak%20Authorization%20with%20ASP.pdf)

## Prerequisites

- .NET 6.0 or later
- ASP.NET Core 8.0 setup
- Keycloak 25.0.2

## ASP.NET Core Setup

Follow the official [ASP.NET Core tutorial](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-8.0&tabs=visual-studio-code) to set up your first Web API using ASP.NET Core 8.0.

## Keycloak Setup

1. **Download and Install Keycloak:**
   - Download `keycloak-25.0.2.zip` from [here](https://github.com/keycloak/keycloak/releases/download/25.0.2/keycloak-25.0.2.zip).
   - Extract the ZIP file.
   - Run Keycloak in development mode:
     ```bash
     bin\kc.bat start-dev
     ```
2. **Configure Keycloak:**
   - Open a browser and go to `http://localhost:8080/`.
   - Complete the admin setup.
   - Create a new realm.
   - Create a client with:
     - Client authentication enabled.
     - A redirect URI to allow your API to receive authentication responses from Keycloak.
   - Retrieve the client secret from the ‘Credentials’ tab.
   - Create a user and set a password.
   - Return to the newly created client, create a role, and assign the user to the role.

## ASP.NET Core Integration with Keycloak

### 1. Install .NET Packages
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   dotnet add package Microsoft.IdentityModel.Protocols.OpenIdConnect
   ```

### 2. Configure `appsettings.json`
   Add the following configuration:
   ```json
   {
     "Authentication": {
       "Keycloak": {
         "Authority": "http://localhost:8080/realms/todorealm",
         "Audience": "account",
         "RequireHttpsMetadata": false
       }
     }
   }
   ```

### 3. Update `Program.cs`
   Modify `Program.cs` to configure authentication and authorization:
   ```csharp
   using Microsoft.AspNetCore.Authentication.JwtBearer;
   using Microsoft.EntityFrameworkCore;
   using Microsoft.IdentityModel.Tokens;

   var builder = WebApplication.CreateBuilder(args);

   ConfigureServices(builder.Services, builder.Configuration);

   var app = builder.Build();

   ConfigureMiddleware(app);

   app.Run();

   void ConfigureServices(IServiceCollection services, IConfiguration configuration)
   {
       services.AddControllers();
       services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));
       services.AddEndpointsApiExplorer();
       services.AddSwaggerGen();
       ConfigureAuthentication(services, configuration); // <-- MODIFICATION
       services.AddAuthorization();
   }

   //MODIFICATION
   void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
   {
       var keycloakSettings = configuration.GetSection("Authentication:Keycloak");
       var authority = keycloakSettings["Authority"];
       var audience = keycloakSettings["Audience"];

       services.AddAuthentication(options =>
       {
           options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
           options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
       })
       .AddJwtBearer(options =>
       {
           options.Authority = authority;
           options.Audience = audience;
           options.RequireHttpsMetadata = false;
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

   void ConfigureMiddleware(WebApplication app)
   {
       if (app.Environment.IsDevelopment())
       {
           app.UseSwagger();
           app.UseSwaggerUI();
       }

       app.UseHttpsRedirection();
       app.UseAuthentication();// <-- MODIFICATION
       app.UseAuthorization();
       app.MapControllers();
   }
   ```

### 4. Add `[Authorize]` Attribute to Controllers
   Apply the `[Authorize]` attribute to relevant controllers.

## Adding Authorization Policies

1. **Configure Authorization Policies:**
   Add the following method to `Program.cs`:
   ```csharp
   void ConfigureAuthorization(IServiceCollection services, IConfiguration configuration)
   {
       services.AddAuthorization(opt =>
       {
           opt.AddPolicy("Policy1", policy =>
           {
               policy.RequireAuthenticatedUser();
               policy.RequireAssertion(context =>
               {
                   // Policy Conditions
               });
           });

           opt.AddPolicy("Policy2", policy =>
           {
               policy.RequireRole("role_1");
           });
       });
   }
   ```

2. **Update `ConfigureServices`:**
   Replace `services.AddAuthorization();` with `ConfigureAuthorization(services, configuration);`.

3. **Apply Policies to Controllers:**
   Replace `[Authorize]` with `[Authorize(Policy = "PolicyX")]` in your controllers.

## Testing the API

### Example POST Request for Token Retrieval
```bash
curl -X POST "http://localhost:8080/realms/<your-realm>/protocol/openid-connect/token" \
-H "Content-Type: application/x-www-form-urlencoded" \
-d "client_id=<your-client-id>" \ 
-d "client_secret=<your-client-secret>" \
-d "grant_type=password" \
-d "username=<username>" \
-d "password=<user-password>"
```

### Example POST Request to the ASP.NET Core API (with authentication)
```bash
curl -X POST 'http://localhost:5272/api/TodoItems' \
-H 'accept: text/plain' \
-H 'Content-Type: application/json' \
-H 'Authorization: Bearer <access-token>' \
-d '{"id":0,"name":"string","isComplete":true}'
```

### Example GET Request to the ASP.NET Core API (with authentication)
```bash
curl -X GET 'http://localhost:5272/api/TodoItems' \
-H 'accept: text/plain' \
-H 'Authorization: Bearer <access-token>'
```

I have provide a python script [RequestScript.py](https://github.com/Basemism/KeyCloak-ASP.NET-Core-API-Integration/blob/main/RequestScript.py) that sends 10 POST and 1 GET requests, depending on your policies, users may receive various responses.
