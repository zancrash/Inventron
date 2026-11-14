using System.Net;
using System.Net.Http.Json;
using InventronAPI.IntegrationTests.Infrastructure;
using InventronAPI.Models;
using InventronAPI.Models.DTOs;
using InventronAPI.Services;

namespace InventronAPI.IntegrationTests.Items
{
	// Required test 1: tenant A cannot read, update or delete tenant B's data.
	public class ItemTenantIsolationTests(ApiFactory factory)
	{
		private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

		[Fact]
		public async Task Other_tenant_cannot_read_update_or_delete_an_item()
		{
			var owner = factory.CreateClientForTenant(Guid.NewGuid());
			var intruder = factory.CreateClientForTenant(Guid.NewGuid());

			var created = await CreateItemAsync(owner, "ISO-1", "Original name");

			// Read: by id and in the list.
			var getResponse = await intruder.GetAsync($"/api/items/{created.Id}", _ct);
			Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

			var intruderList = await intruder.GetFromJsonAsync<PagedResult<ItemDto>>("/api/items", _ct);
			Assert.NotNull(intruderList);
			Assert.Equal(0, intruderList.Total);
			Assert.Empty(intruderList.Items);

			// Update.
			var update = new ItemDto
			{
				Id = created.Id,
				Sku = created.Sku,
				Name = "Hijacked",
				Type = created.Type,
				UnitOfMeasure = created.UnitOfMeasure,
				StandardCost = created.StandardCost,
			};
			var putResponse = await intruder.PutAsJsonAsync($"/api/items/{created.Id}", update, _ct);
			Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);

			// Delete.
			var deleteResponse = await intruder.DeleteAsync($"/api/items/{created.Id}", _ct);
			Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

			// The owner still sees the item, unchanged.
			var ownerView = await owner.GetFromJsonAsync<ItemDto>($"/api/items/{created.Id}", _ct);
			Assert.NotNull(ownerView);
			Assert.Equal("Original name", ownerView.Name);
		}

		private async Task<ItemDto> CreateItemAsync(HttpClient client, string sku, string name)
		{
			var response = await client.PostAsJsonAsync("/api/items/add", TestData.NewItem(sku, name), _ct);
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var item = await response.Content.ReadFromJsonAsync<ItemDto>(_ct);
			Assert.NotNull(item);
			return item;
		}
	}
}
