// ── FILE: src/TaskBoard.API/Program.cs ───────────────────────────────────────
//
// Program.cs is the COMPOSITION ROOT of the application.
// ───────────────────────────────────────────────────────── The Composition Root is the one place
// in the application where all dependencies are wired together. In Clean Architecture, this is the
// ONLY place that knows about ALL layers — it ties everything together.
//
// WHY does Program.cs reference Infrastructure? ────────────────────────────────────────────── Even
// though controllers never touch Infrastructure directly, we need to register Infrastructure
// services in the DI container. The API's Program.cs is the Composition Root, so it's allowed to
// know about all layers for wiring purposes.
//
// WHY each service registration matters: See inline comments below for each registration. ─────────────────────────────────────────────────────────────────────────────

using TaskBoard.API.Middleware;
using TaskBoard.Application;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════════ SERVICE REGISTRATION ══════════════════════════════════════════════════════════════════════

// ── 1. Add Controllers ── Registers MVC controllers for handling HTTP requests. We use
// .AddControllers() (not .AddMvc()) because we're building a pure API — no views, no Razor pages.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ── Serialize enums as strings instead of integers ── This makes API responses more
        // readable: "status": "Pending" instead of "status": 0
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── 2. Add Swagger/OpenAPI ── Provides interactive API documentation at /swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Task Board API",
        Version = "v1",
        Description = "A Clean Architecture + CQRS + MediatR reference project for learning."
    });
});

// ── 3. Register Application Layer Services ── WHY this is called here: This registers MediatR
// (mediator pattern), FluentValidation (input validation), and all 4 pipeline behaviors in the
// correct order:
// 1. LoggingBehavior → 2. ValidationBehavior → 3. TransactionBehavior → 4. PerformanceBehavior
//
// The ORDER of behavior registration determines the ORDER they execute. Getting this wrong would
// silently break cross-cutting concerns.
builder.Services.AddApplicationServices();

// ── 4. Register Infrastructure Layer Services ── WHY this is called here: This registers the EF
// Core DbContext (with SQL Server connection), the IProjectRepository implementation, and the ITransactionManager.
//
// The connection string comes from appsettings.json. If it's missing, we use a default LocalDB
// connection string for development.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb;Trusted_Connection=true;MultipleActiveResultSets=true";

builder.Services.AddInfrastructureServices(connectionString);

// ══════════════════════════════════════════════════════════════════════ MIDDLEWARE PIPELINE ══════════════════════════════════════════════════════════════════════

var app = builder.Build();

// ── Apply EF Core Migrations Automatically (development convenience) ── WHY auto-migrate? For
// educational/development purposes, this ensures the database is always up-to-date without running
// migration commands manually. In PRODUCTION, you'd use explicit migration scripts instead.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// ── Global Exception Handling Middleware ── WHY this is FIRST in the pipeline: It wraps the entire
// pipeline, so it can catch ANY exception from any layer (controller, handler, domain, validator,
// etc.) and convert it to a proper HTTP error response.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// ── Swagger ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ── Map controller routes ──
app.MapControllers();

// ── Run the application ──
await app.RunAsync();