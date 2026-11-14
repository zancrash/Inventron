using InventronAPI.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(InventronAPI.IntegrationTests.Infrastructure.ApiFactory))]

namespace InventronAPI.IntegrationTests.Infrastructure
{
	/// <summary>
	/// Hosts the real API against a throwaway PostgreSQL container, shared by every test in the run.
	/// Tests stay independent by using a fresh tenant each, so the database is never reset between tests.
	/// </summary>
	public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
	{
		// Same major version as docker-compose.yml.
		private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18").Build();

		public async ValueTask InitializeAsync()
		{
			await _db.StartAsync();

			using var scope = Services.CreateScope();
			await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			// Not Development, so the DevTenantId fallback is off and a request without a tenant fails closed.
			builder.UseEnvironment("Testing");
			builder.UseSetting("ConnectionStrings:LocalConnection", _db.GetConnectionString());
			builder.UseSetting("Logging:LogLevel:Default", "Warning");

			builder.ConfigureTestServices(services =>
			{
				services.AddAuthentication(TestAuthHandler.SchemeName)
					.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
			});
		}

		/// <summary>An HTTP client whose requests are authenticated as a user of <paramref name="tenantId"/>.</summary>
		public HttpClient CreateClientForTenant(Guid tenantId)
		{
			var client = CreateClient();
			client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, tenantId.ToString());
			return client;
		}

		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync();
			await _db.DisposeAsync();
		}
	}
}
