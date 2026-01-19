namespace TodoList.Domain.Entities;

public class TodoItem
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
