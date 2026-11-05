namespace InventronAPI.Models.Entities
{
	public class Item : TenantEntity
	{
		public required string Sku { get; set; }

		public required string Name { get; set; }

		public string? Description { get; set; }

		public E_ItemType Type { get; set; }

		public required string UnitOfMeasure { get; set; }

		public required decimal StandardCost { get; set; }

		public decimal? SellingPrice { get; set; }

		public int? LeadTimeDays { get; set; }

		public decimal? SafetyStock { get; set; }

		public decimal? MinOrderQty { get; set; }

		public decimal? LotSize { get; set; }

		public int? PreferredSupplierId { get; set; }

		public Supplier? PreferredSupplier { get; set; }

		public bool IsActive { get; set; } = true;

		public byte[] RowVersion { get; set; } = [];
	}
}
