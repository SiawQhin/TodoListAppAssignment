using System.ComponentModel.DataAnnotations;

namespace TodoList.Application.DTOs;

public class CreateTodoRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 500 characters")]
    public string Title { get; set; } = string.Empty;
}
