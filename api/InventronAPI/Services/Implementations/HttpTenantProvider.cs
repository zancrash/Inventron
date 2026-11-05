using InventronAPI.Services.Interfaces;

namespace InventronAPI.Services.Implementations
{
	public class HttpTenantProvider : ITenantProvider
	{
		public const string TenantClaimType = "tenant_id";

		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly Guid? _devTenantId;

		public HttpTenantProvider(IHttpContextAccessor httpContextAccessor, IConfiguration config, IHostEnvironment env)
		{
			_httpContextAccessor = httpContextAccessor;

			// Temporary until auth exists: in Development only, requests without a tenant_id claim use Tenancy:DevTenantId.
			if (env.IsDevelopment() && Guid.TryParse(config["Tenancy:DevTenantId"], out var devTenantId))
			{
				_devTenantId = devTenantId;
			}
		}

		public Guid? TenantId
		{
			get
			{
				var claim = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType)?.Value;
				return Guid.TryParse(claim, out var tenantId) ? tenantId : _devTenantId;
			}
		}
	}
}
