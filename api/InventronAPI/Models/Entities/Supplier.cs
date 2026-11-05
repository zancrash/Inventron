namespace InventronAPI.Models.Entities
{
	public class Supplier : TenantEntity
	{
		public required string Company { get; set; }

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public bool IsActive { get; set; } = true;
	}
}
