using System.ComponentModel.DataAnnotations;

namespace InventronAPI.Models
{
	public class ItemQuery
	{
		public string? Search { get; set; }

		[Range(1, int.MaxValue)]
		public int Page { get; set; } = 1;

		[Range(1, 200)]
		public int PageSize { get; set; } = 50;
	}
}
