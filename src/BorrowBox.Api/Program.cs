using BorrowBox.Api.Borrowers;
using BorrowBox.Api.Equipment;
using BorrowBox.Api.Errors;
using BorrowBox.Api.Loans;
using BorrowBox.Api.Inspections;
using BorrowBox.Api.Reservations;
using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Loans;
using BorrowBox.Core.Inspections;
using BorrowBox.Core.Reservations;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Loans;
using BorrowBox.Infrastructure.Inspections;
using BorrowBox.Infrastructure.Reservations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BorrowBoxExceptionHandler>();
builder.Services.AddSingleton<IEquipmentStore, InMemoryEquipmentStore>();
builder.Services.AddSingleton<IBorrowerStore, InMemoryBorrowerStore>();
builder.Services.AddSingleton<ILoanStore, InMemoryLoanStore>();
builder.Services.AddSingleton(new BorrowerLoanPolicy(maximumActiveLoans: 3));
builder.Services.AddSingleton<IInspectionStore, InMemoryInspectionStore>();
builder.Services.AddSingleton<IReservationStore, InMemoryReservationStore>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<BorrowerService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ReservationExpiryService>();
builder.Services.AddHostedService<ReservationExpiryWorker>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapEquipmentEndpoints();
app.MapBorrowerEndpoints();
app.MapLoanEndpoints();
app.MapInspectionEndpoints();
app.MapReservationEndpoints();

app.Run();

public partial class Program;
