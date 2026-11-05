using InventronAPI.Models;
using InventronAPI.Models.DTOs;
using static InventronAPI.Services.Implementations.ItemsService;

namespace InventronAPI.Services.Interfaces
{
	public interface IItemsService
	{
		Task<PagedResult<ItemDto>> GetItemsAsync(ItemQuery q, CancellationToken ct = default);

		Task<ItemDto?> GetByIdAsync(int id, CancellationToken ct = default);

		Task<ItemDto> AddItemAsync(NewItemRequestDto item, CancellationToken ct = default);

		Task<ImportResult> ImportItemsAsync(List<NewItemRequestDto> items, bool dryRun, CancellationToken ct = default);

		Task<bool> UpdateItemAsync(int id, ItemDto item, CancellationToken ct = default);
	
		Task<bool> DeleteItemAsync(int id, CancellationToken ct = default);
	}
}
