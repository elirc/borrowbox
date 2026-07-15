using BorrowBox.Api.Borrowers;
using BorrowBox.Api.Equipment;
using BorrowBox.Api.Errors;
using BorrowBox.Api.Loans;
using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Loans;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Loans;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BorrowBoxExceptionHandler>();
builder.Services.AddSingleton<IEquipmentStore, InMemoryEquipmentStore>();
builder.Services.AddSingleton<IBorrowerStore, InMemoryBorrowerStore>();
builder.Services.AddSingleton<ILoanStore, InMemoryLoanStore>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<BorrowerService>();
builder.Services.AddScoped<CheckoutService>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapEquipmentEndpoints();
app.MapBorrowerEndpoints();
app.MapLoanEndpoints();

app.Run();

public partial class Program;
