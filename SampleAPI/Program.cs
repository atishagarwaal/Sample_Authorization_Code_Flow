using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        // The URL of your Duende IdentityServer
        options.Authority = "https://localhost:5001";

        // Enable validation of the token's audience
        options.TokenValidationParameters.ValidateAudience = true;
        // IdentityServer resolves the audience from scope mappings internally
        // So, the client need not send the audience claim in the token
        options.TokenValidationParameters.ValidAudiences = new List<string> { "api-weather" };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WeatherReadPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.Claims.Any(claim =>
                claim.Type == "scope" &&
                claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Contains("weather.read", StringComparer.Ordinal))));
});

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sample API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sample API"));

}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
