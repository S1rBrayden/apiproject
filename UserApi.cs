
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddLogging();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseSwagger();
app.UseSwaggerUI();

// Middleware for logging
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogger");
    logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next.Invoke();
    logger.LogInformation("Finished handling request.");
});

// In-memory user store
var users = new ConcurrentDictionary<Guid, User>();

// User model with validation
public class User
{
    [Required]
    [MinLength(2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }
}

// Create user
app.MapPost("/users", ([FromBody] User user) =>
{
    var id = Guid.NewGuid();
    users[id] = user;
    return Results.Created($"/users/{id}", new { id, user });
});

// Get user by ID
app.MapGet("/users/{id:guid}", (Guid id) =>
{
    if (users.TryGetValue(id, out var user))
        return Results.Ok(new { id, user });
    return Results.NotFound("User not found");
});

// Update user
app.MapPut("/users/{id:guid}", (Guid id, [FromBody] User updatedUser) =>
{
    if (!users.ContainsKey(id))
        return Results.NotFound("User not found");

    users[id] = updatedUser;
    return Results.Ok(new { id, user = updatedUser });
});

// Delete user
app.MapDelete("/users/{id:guid}", (Guid id) =>
{
    if (users.TryRemove(id, out _))
        return Results.NoContent();
    return Results.NotFound("User not found");
});

// List all users
app.MapGet("/users", () => Results.Ok(users));

app.Run();
