using BorrowBox.Api.Borrowers;
using BorrowBox.Api.Equipment;
using BorrowBox.Api.Errors;
using BorrowBox.Api.Inspections;
using BorrowBox.Api.Loans;
using BorrowBox.Api.Reporting;
using BorrowBox.Api.Reservations;
using BorrowBox.Api.Security;
using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Inspections;
using BorrowBox.Core.Loans;
using BorrowBox.Core.Reporting;
using BorrowBox.Core.Reservations;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Inspections;
using BorrowBox.Infrastructure.Loans;
using BorrowBox.Infrastructure.Reservations;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BorrowBoxExceptionHandler>();
builder.Services.AddAuthentication(BorrowBoxSecurity.Scheme)
    .AddScheme<AuthenticationSchemeOptions, DemoHeaderAuthenticationHandler>(BorrowBoxSecurity.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
    options.AddPolicy(BorrowBoxSecurity.StaffPolicy, policy => policy.RequireRole("Staff")));
builder.Services.AddSingleton<IEquipmentStore, InMemoryEquipmentStore>();
builder.Services.AddSingleton<IBorrowerStore, InMemoryBorrowerStore>();
builder.Services.AddSingleton<ILoanStore, InMemoryLoanStore>();
builder.Services.AddSingleton(new BorrowerLoanPolicy(maximumActiveLoans: 3));
builder.Services.AddSingleton<IClock, BorrowBox.Core.Common.SystemClock>();
builder.Services.AddSingleton<IInspectionStore, InMemoryInspectionStore>();
builder.Services.AddSingleton<IReservationStore, InMemoryReservationStore>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<BorrowerService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ReservationExpiryService>();
builder.Services.AddScoped<InventoryReportService>();
builder.Services.AddHostedService<ReservationExpiryWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100)
        correlationId = context.TraceIdentifier;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        await next(context);
});
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing")) app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new
{
    status = "ready",
    persistence = "in-memory",
    durable = false,
    note = "SQLite adapter is available but not selected by the teaching default."
}));
app.MapEquipmentEndpoints();
app.MapBorrowerEndpoints();
app.MapLoanEndpoints();
app.MapInspectionEndpoints();
app.MapReservationEndpoints();
app.MapReportingEndpoints();

app.Run();

public partial class Program;
