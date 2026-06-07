using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Application.Features.Warehouses.Commands.CreateWarehouse;
using Application.Features.Warehouses.Commands.UpdateWarehouse;
using Application.Features.Inventories.Commands.AddInventory;

namespace Application.Mappings
{
    public class WarehouseProfile : Profile
    {
        public WarehouseProfile()
        {
            CreateMap<Warehouse, WarehouseDTO>();
            CreateMap<CreateWarehouseCommand, Warehouse>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore());
            CreateMap<UpdateWarehouseCommand, Warehouse>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore());
            CreateMap<AddInventoryCommand, Inventory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.ProductName, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            CreateMap<Inventory, InventoryDTO>();
            CreateMap<StockReservation, StockReservationDTO>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : "Unknown Warehouse"));
        }
    }
}
