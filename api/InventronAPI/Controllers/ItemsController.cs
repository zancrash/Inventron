using InventronAPI.Models;
using InventronAPI.Models.DTOs;
using InventronAPI.Services;
using InventronAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventronAPI.Controllers
{
	// localhost:xxxx/api/Items
	[Route("api/[controller]")]
	[ApiController]
	public class ItemsController : ControllerBase
	{
		private readonly IItemsService _itemsService;
		public ItemsController(IItemsService itemsService)
		{
			_itemsService = itemsService;
		}

		[HttpGet]
		public async Task<ActionResult<PagedResult<ItemDto>>> Get([FromQuery] ItemQuery q, CancellationToken ct)
		{
			return Ok(await _itemsService.GetItemsAsync(q, ct));
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<ItemDto?>> GetById(int id, CancellationToken ct)
		{
			var product = await _itemsService.GetByIdAsync(id, ct);

			if (product is null)
				return NotFound();

			return Ok(product);
		}

		[HttpPost("Add")]
		public async Task<ActionResult<ItemDto>> AddItem(NewItemRequestDto item, CancellationToken ct)
		{
			var addedProduct = await _itemsService.AddItemAsync(item, ct);
			return CreatedAtAction(nameof(GetById), new { id = addedProduct.Id }, addedProduct);
		}

		[HttpPost("Import")]
		[ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ImportResult), StatusCodes.Status422UnprocessableEntity)]
		public async Task<ActionResult<ImportResult>> ImportItems(List<NewItemRequestDto> items, [FromQuery] bool dryRun = false, CancellationToken ct = default)
		{
			if (items.Count == 0)
			{
				return BadRequest(new ProblemDetails { Title = "No items provided for import." });
			}

			if (items.Count > 5000)
			{
				return BadRequest(new ProblemDetails { Title = "Imports are limited to 5000 rows." });
			}

			var result = await _itemsService.ImportItemsAsync(items, dryRun, ct);

			return result.Errors.Count > 0 ? UnprocessableEntity(result) : Ok(result);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateItem(int id, ItemDto item, CancellationToken ct)
		{
			var updated = await _itemsService.UpdateItemAsync(id, item, ct);
			return updated ? NoContent() : NotFound("Given item was not found.");
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteItem(int id, CancellationToken ct) 
		{
			var deleted = await _itemsService.DeleteItemAsync(id, ct);
			return deleted ? NoContent() : NotFound("Given item was not found.");
		}
	}
}
