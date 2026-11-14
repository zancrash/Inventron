using System.Security.Claims;
using System.Text.Encodings.Web;
using InventronAPI.Services.Implementations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventronAPI.IntegrationTests.Infrastructure
{
	/// <summary>
	/// Stands in for real auth: turns the X-Test-Tenant header into a principal carrying the tenant_id claim,
	/// so requests flow through the production HttpTenantProvider and query filters.
	/// </summary>
	public sealed class TestAuthHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder)
		: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		public const string SchemeName = "Test";
		public const string TenantHeader = "X-Test-Tenant";

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Request.Headers.TryGetValue(TenantHeader, out var tenant))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			var identity = new ClaimsIdentity(
				[new Claim(HttpTenantProvider.TenantClaimType, tenant.ToString())],
				SchemeName);

			return Task.FromResult(AuthenticateResult.Success(
				new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
		}
	}
}
