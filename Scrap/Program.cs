using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
{
    builder.Services.AddControllers();
    builder.Services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase("TodoList"));
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAuthentication(opt => 
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer( opt =>
        {
            opt.Authority = builder.Configuration["Keycloak:Authority"];
            opt.RequireHttpsMetadata = false;
            opt.Audience = builder.Configuration["Keycloak:Audience"];
            opt.TokenValidationParameters = new TokenValidationParameters 
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateLifetime = true
            };
        });

var app = builder.Build();
// Configure the HTTP request pipeline.
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API");
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();
}

app.Run();
}
