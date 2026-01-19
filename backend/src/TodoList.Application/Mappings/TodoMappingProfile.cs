using AutoMapper;
using TodoList.Application.DTOs;
using TodoList.Domain.Entities;

namespace TodoList.Application.Mappings;

public class TodoMappingProfile : Profile
{
    public TodoMappingProfile()
    {
        // Entity to DTO
        CreateMap<TodoItem, TodoItemDto>();

        // DTO to Entity
        CreateMap<CreateTodoRequest, TodoItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<UpdateTodoRequest, TodoItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }
}
