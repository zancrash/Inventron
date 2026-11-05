using AutoMapper;
using AutoMapper.QueryableExtensions;
using InventronAPI.Data;
using InventronAPI.Models;
using InventronAPI.Models.DTOs;
using InventronAPI.Models.Entities;
using InventronAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventronAPI.Services.Implementations
{
	public class ItemsService : IItemsService
	{
		private readonly AppDbContext _ctx;
		private readonly IMapper _mapper;

		public ItemsService(AppDbContext ctx, IMapper mapper)
		{
			_ctx = ctx;
			_mapper = mapper;
		}

		public async Task<PagedResult<ItemDto>> GetItemsAsync(ItemQuery q, CancellationToken ct = default)
		{
			var query = _ctx.Items.AsNoTracking().Where(i => i.IsActive);

			if (!string.IsNullOrWhiteSpace(q.Search))
			{
				query = query.Where(x => x.Sku.Contains(q.Search) || x.Name.Contains(q.Search));
			}

			var total = await query.CountAsync(ct);
			var items = await query
				.OrderBy(x => x.Sku)
				.Skip((q.Page - 1) * q.PageSize)
				.Take(Math.Min(q.PageSize, 200))
				.ProjectTo<ItemDto>(_mapper.ConfigurationProvider)
				.ToListAsync(ct);

			return new PagedResult<ItemDto>(items, total, q.Page, q.PageSize);
		}

		public async Task<ItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
		{
			var itemEntity = await _ctx.Items.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
			return itemEntity is null ? null : _mapper.Map<ItemDto>(itemEntity);
		}

		public async Task<ItemDto> AddItemAsync(NewItemRequestDto item, CancellationToken ct = default)
		{
			var newItem = _mapper.Map<Item>(item);

			_ctx.Items.Add(newItem);
			await _ctx.SaveChangesAsync(ct);

			return _mapper.Map<ItemDto>(newItem);
		}

		public async Task<ImportResult> ImportItemsAsync(List<NewItemRequestDto> items, bool dryRun, CancellationToken ct = default)
		{
			var errors = new List<ImportRowError>();

			var skus = items.Select(i => i.Sku.Trim().ToUpperInvariant()).ToList();

			foreach (var group in skus.Select((sku, index) => (sku, index)).GroupBy(x => x.sku).Where(group => group.Count() > 1))
			{
				foreach(var (sku, index) in group.Skip(1))
				{
					errors.Add(new ImportRowError(index + 1, sku, "Duplicate SKU in import file."));
				}
			}

			var existing = await _ctx.Items.Where(i => skus.Contains(i.Sku)).Select(i => i.Sku).ToListAsync(ct);

			if (errors.Count > 0)
			{
				return new ImportResult(0, errors);
			}

			if (dryRun) return new(items.Count, errors);   // report what would be created, write nothing

			_ctx.Items.AddRange(_mapper.Map<List<Item>>(items));

			await _ctx.SaveChangesAsync(ct);

			return new ImportResult(items.Count, errors);
		}

		public async Task<bool> UpdateItemAsync(int id, ItemDto item, CancellationToken ct = default)
		{
			if (id != item.Id) return false;

			var itemToUpdate = await _ctx.Items.FindAsync(new object[] { id }, ct);

			if (itemToUpdate == null) 
			{
				return false;
			}

			_mapper.Map(item, itemToUpdate);
			
			await _ctx.SaveChangesAsync(ct);

			return true;
		}

		public async Task<bool> DeleteItemAsync(int id, CancellationToken ct = default)
		{
			var itemToDelete = await _ctx.Items.FindAsync(new object[] { id }, ct);

			if (itemToDelete == null)
			{
				return false;
			}

			_ctx.Items.Remove(itemToDelete);

			await _ctx.SaveChangesAsync(ct);

			return true;
		}
	}
}
