using Application.Interfaces;
using Application.DTOs;
using Domain.Interfaces;

namespace Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;
        public WarehouseService(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }

        public async Task<bool> CreateWarehouseAsync(WarehouseDTO warehouseDto)
        {
            var warehouse = new Domain.Entities.Warehouse
            {
                Id = Guid.NewGuid().ToString(),
                Name = warehouseDto.Name,
                Address = warehouseDto.Address,
                Capacity = warehouseDto.Capacity

            };
            await _warehouseRepository.AddAsync(warehouse);
            return await _warehouseRepository.SaveChangeAsync();
        }
    }
}
