namespace InventronAPI.Services.Interfaces
{
	public interface ITenantProvider
	{
		/// <summary>The current request's tenant, or null when none is known (no tenant_id claim).</summary>
		Guid? TenantId { get; }
	}
}
