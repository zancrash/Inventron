using AutoMapper;
using InventronAPI.Models.DTOs;
using InventronAPI.Models.Entities;

namespace InventronAPI.Services.Utils
{
	public class ServiceMapper : Profile
	{
		public ServiceMapper()
		{
			CreateMap<Item, ItemDto>().ReverseMap();
			CreateMap<Item, NewItemRequestDto>().ReverseMap();
		}
	}
}
