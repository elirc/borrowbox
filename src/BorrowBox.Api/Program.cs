using BorrowBox.Api.Borrowers;
using BorrowBox.Api.Equipment;
using BorrowBox.Api.Errors;
using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Equipment;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BorrowBoxExceptionHandler>();
builder.Services.AddSingleton<IEquipmentStore, InMemoryEquipmentStore>();
builder.Services.AddSingleton<IBorrowerStore, InMemoryBorrowerStore>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<BorrowerService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapEquipmentEndpoints();
app.MapBorrowerEndpoints();

app.Run();

public partial class Program;
