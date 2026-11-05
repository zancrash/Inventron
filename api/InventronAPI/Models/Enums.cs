namespace InventronAPI.Models
{
	public enum E_ItemType { 
		RawMaterial = 1, 
		SubAssembly = 2, 
		FinishedGood = 3, 
		Purchased = 4 
	}

	public enum E_UnitOfMeasure
	{
		Each = 1,
		Kg = 2,
		Lbs = 3,
	}

	public enum E_InventoryTransactionType { 
		Receipt = 1, 
		Issue = 2, 
		Adjustment = 3, 
		ProductionReceipt = 4, 
		ProductionIssue = 5 
	}


}
