using BorrowBox.Api.Equipment;
using BorrowBox.Api.Errors;
using BorrowBox.Core.Equipment;
using BorrowBox.Infrastructure.Equipment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BorrowBoxExceptionHandler>();
builder.Services.AddSingleton<IEquipmentStore, InMemoryEquipmentStore>();
builder.Services.AddScoped<EquipmentService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapEquipmentEndpoints();

app.Run();

public partial class Program;
