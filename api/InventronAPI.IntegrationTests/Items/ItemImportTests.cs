using System.Net;
using System.Net.Http.Json;
using InventronAPI.IntegrationTests.Infrastructure;
using InventronAPI.Models.DTOs;
using InventronAPI.Services;

namespace InventronAPI.IntegrationTests.Items
{
	// Required test 3: an import with one bad row creates nothing and reports the correct row number.
	// Row numbers assume row 1 of the CSV is the header, so the item at index i is row i + 2.
	public class ItemImportTests(ApiFactory factory)
	{
		private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

		[Fact]
		public async Task Duplicate_sku_within_the_file_creates_nothing_and_reports_its_row()
		{
			var client = factory.CreateClientForTenant(Guid.NewGuid());
			List<NewItemRequestDto> rows =
			[
				TestData.NewItem("IMP-1"), // row 2
				TestData.NewItem("IMP-2"), // row 3
				TestData.NewItem("imp-1"), // row 4: duplicate of row 2 after normalisation
			];

			var response = await client.PostAsJsonAsync("/api/items/import", rows, _ct);

			Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
			var result = await response.Content.ReadFromJsonAsync<ImportResult>(_ct);
			Assert.NotNull(result);
			Assert.Equal(0, result.Created);
			var error = Assert.Single(result.Errors);
			Assert.Equal(4, error.Row);

			await AssertNoItemsAsync(client);
		}

		[Fact]
		public async Task Sku_that_already_exists_creates_nothing_and_reports_its_row()
		{
			var client = factory.CreateClientForTenant(Guid.NewGuid());
			var existing = await client.PostAsJsonAsync("/api/items/add", TestData.NewItem("EXIST-1"), _ct);
			Assert.Equal(HttpStatusCode.Created, existing.StatusCode);

			List<NewItemRequestDto> rows =
			[
				TestData.NewItem("NEW-1"),   // row 2
				TestData.NewItem("EXIST-1"), // row 3: already in the database
			];

			var response = await client.PostAsJsonAsync("/api/items/import", rows, _ct);

			Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
			var result = await response.Content.ReadFromJsonAsync<ImportResult>(_ct);
			Assert.NotNull(result);
			var error = Assert.Single(result.Errors);
			Assert.Equal(3, error.Row);

			var items = await client.GetFromJsonAsync<PagedResult<ItemDto>>("/api/items", _ct);
			Assert.NotNull(items);
			Assert.Equal("EXIST-1", Assert.Single(items.Items).Sku);
		}

		private async Task AssertNoItemsAsync(HttpClient client)
		{
			var items = await client.GetFromJsonAsync<PagedResult<ItemDto>>("/api/items", _ct);
			Assert.NotNull(items);
			Assert.Equal(0, items.Total);
		}
	}
}
