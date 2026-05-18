using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    public class CatalogProfile : Profile
    {
        public CatalogProfile() 
        { 
            CreateMap<Product, ProductDTO>();
            CreateMap<CreateProductDTO, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<UpdateProductDTO, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt =>
                {
                    opt.PreCondition(src => src.Price.HasValue);
                    opt.MapFrom(src => src.Price!.Value);
                })
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) 
                        return false;
                    if (srcMember is string str && string.IsNullOrWhiteSpace(str)) 
                        return false;
                    return true;
                }));
            CreateMap<Category, CategoryDTO>();
            CreateMap<CreateCategoryDTO, Category>();
            CreateMap<UpdateCategoryDTO, Category>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) 
                        return false;
                    if (srcMember is string str && string.IsNullOrWhiteSpace(str)) 
                        return false;
                    return true;
                }));
        }
    }
}
