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
			var pattern = $"%{q.Search}%";
			var query = _ctx.Items.AsNoTracking().Where(i => i.IsActive);

			if (!string.IsNullOrWhiteSpace(q.Search))
			{
				query = query.Where(x => EF.Functions.ILike(x.Sku, pattern) || EF.Functions.ILike(x.Name, pattern));
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
			// Row 1 of the file is the header, so the item at index i is row i + 2.
			static int RowNumber(int index) => index + 2;

			var errors = new List<ImportRowError>();

			var skus = items.Select(i => (i.Sku ?? "").Trim().ToUpperInvariant()).ToList();

			var firstRowBySku = new Dictionary<string, int>();
			for (var i = 0; i < skus.Count; i++)
			{
				if (skus[i].Length == 0)
				{
					errors.Add(new ImportRowError(RowNumber(i), skus[i], "SKU is required."));
				}
				else if (firstRowBySku.TryGetValue(skus[i], out var firstRow))
				{
					errors.Add(new ImportRowError(RowNumber(i), skus[i], $"Duplicate SKU in import file (first seen on row {firstRow})."));
				}
				else
				{
					firstRowBySku[skus[i]] = RowNumber(i);
				}
			}

			// One WHERE IN query. Upper-case the column because rows added outside the import may not be normalised.
			var candidateSkus = firstRowBySku.Keys.ToList();
			var existing = await _ctx.Items
				.Where(i => candidateSkus.Contains(i.Sku.ToUpper()))
				.Select(i => i.Sku.ToUpper())
				.ToListAsync(ct);

			foreach (var sku in existing.Distinct())
			{
				errors.Add(new ImportRowError(firstRowBySku[sku], sku, "An item with this SKU already exists."));
			}

			if (errors.Count > 0)
			{
				return new ImportResult(0, errors.OrderBy(e => e.Row).ToList());
			}

			if (dryRun) return new(items.Count, errors);   // report what would be created, write nothing

			var newItems = _mapper.Map<List<Item>>(items);
			for (var i = 0; i < newItems.Count; i++)
			{
				newItems[i].Sku = skus[i];
			}

			// All or nothing: one SaveChanges. The unique index remains the backstop for concurrent imports.
			_ctx.Items.AddRange(newItems);

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
