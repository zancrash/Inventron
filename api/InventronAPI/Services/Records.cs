namespace InventronAPI.Services
{
	public record ImportRowError(int Row, string Sku, string Message);
	public record ImportResult(int Created, List<ImportRowError> Errors);

	public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
}
