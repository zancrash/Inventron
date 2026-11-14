using InventronAPI.Models;
using InventronAPI.Models.DTOs;

namespace InventronAPI.IntegrationTests.Infrastructure
{
	public static class TestData
	{
		public static NewItemRequestDto NewItem(string sku, string name = "Test item") => new()
		{
			Sku = sku,
			Name = name,
			Type = E_ItemType.Purchased,
			UnitOfMeasure = "EA",
			StandardCost = 1.25m,
		};
	}
}
