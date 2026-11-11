using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventronAPI.Infrastructure
{
	// Turns known exceptions into ProblemDetails responses. Anything not handled here falls through to a 500.
	// TODO: add domain exceptions (not found → 404, validation → 400/422) when services stop returning bool.
	public class GlobalExceptionHandler : IExceptionHandler
	{
		private const string UniqueViolation = "23505";

		private readonly IProblemDetailsService _problemDetails;
		private readonly ILogger<GlobalExceptionHandler> _logger;

		public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
		{
			_problemDetails = problemDetails;
			_logger = logger;
		}

		public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
		{
			var problem = exception switch
			{
				// Must come before DbUpdateException, which it derives from.
				DbUpdateConcurrencyException => new ProblemDetails
				{
					Status = StatusCodes.Status409Conflict,
					Title = "The record was changed by someone else.",
					Detail = "Reload it and try again.",
				},
				DbUpdateException { InnerException: PostgresException { SqlState: UniqueViolation } pg } => new ProblemDetails
				{
					Status = StatusCodes.Status409Conflict,
					Title = "A record with the same value already exists.",
					Extensions = { ["constraint"] = pg.ConstraintName },
				},
				_ => null,
			};

			if (problem is null) return false;

			_logger.LogInformation(exception, "Request failed with {StatusCode}: {Title}", problem.Status, problem.Title);

			httpContext.Response.StatusCode = problem.Status!.Value;
			return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
			{
				HttpContext = httpContext,
				ProblemDetails = problem,
				Exception = exception,
			});
		}
	}
}
