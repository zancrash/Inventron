namespace InventronAPI.Models.Entities
{
	public class InventoryTransaction : TenantEntity
	{
		public int ItemId { get; set; }

		public Item Item { get; set; } = null!;

		public E_InventoryTransactionType TransactionType { get; set; }

		public decimal Quantity { get; set; }

		public string? Reference { get; set; }

		public string? Notes { get; set; }
	}
}
