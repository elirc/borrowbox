using BorrowBox.Core.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BorrowBox.Api.Errors;

public sealed class BorrowBoxExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BorrowBoxException expected) return false;
        context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "BorrowBox rule violation",
                Detail = expected.Message,
                Extensions = { ["code"] = expected.Code, ["traceId"] = context.TraceIdentifier },
            },
            Exception = exception,
        }).ConfigureAwait(false);
    }
}
