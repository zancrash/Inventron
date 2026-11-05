namespace InventronAPI.Models.Entities
{
	public abstract class TenantEntity
	{
		public int Id { get; set; }

		public required Guid TenantId { get; set; }

		public DateTime CreatedAtUtc { get; set; }

		public DateTime? UpdatedAtUtc { get; set; }
	}
}
