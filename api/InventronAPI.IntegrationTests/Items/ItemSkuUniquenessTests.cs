using System.Net;
using System.Net.Http.Json;
using InventronAPI.IntegrationTests.Infrastructure;

namespace InventronAPI.IntegrationTests.Items
{
	// Required test 2: a duplicate SKU returns 409; the same SKU in two different tenants is allowed.
	public class ItemSkuUniquenessTests(ApiFactory factory)
	{
		private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

		[Theory]
		[InlineData("DUP-1")]
		[InlineData("dup-1")] // SKUs are case-insensitive.
		public async Task Duplicate_sku_in_the_same_tenant_returns_409(string secondSku)
		{
			var client = factory.CreateClientForTenant(Guid.NewGuid());

			var first = await client.PostAsJsonAsync("/api/items/add", TestData.NewItem("DUP-1"), _ct);
			Assert.Equal(HttpStatusCode.Created, first.StatusCode);

			var second = await client.PostAsJsonAsync("/api/items/add", TestData.NewItem(secondSku), _ct);
			Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
		}

		[Fact]
		public async Task Same_sku_in_two_tenants_is_allowed()
		{
			var tenantA = factory.CreateClientForTenant(Guid.NewGuid());
			var tenantB = factory.CreateClientForTenant(Guid.NewGuid());

			var a = await tenantA.PostAsJsonAsync("/api/items/add", TestData.NewItem("SHARED-1"), _ct);
			var b = await tenantB.PostAsJsonAsync("/api/items/add", TestData.NewItem("SHARED-1"), _ct);

			Assert.Equal(HttpStatusCode.Created, a.StatusCode);
			Assert.Equal(HttpStatusCode.Created, b.StatusCode);
		}
	}
}
