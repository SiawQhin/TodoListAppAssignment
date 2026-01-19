using Microsoft.EntityFrameworkCore;
using TodoList.Domain.Entities;
using TodoList.Domain.Repositories;
using TodoList.Infrastructure.Data;

namespace TodoList.Infrastructure.Repositories;

public class EfCoreTodoRepository : ITodoRepository
{
    private readonly ApplicationDbContext _context;

    public EfCoreTodoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync()
    {
        return await _context.TodoItems.ToListAsync();
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id)
    {
        return await _context.TodoItems.FindAsync(id);
    }

    public async Task<TodoItem> AddAsync(TodoItem todoItem)
    {
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        return todoItem;
    }

    public async Task<TodoItem?> UpdateAsync(TodoItem todoItem)
    {
        var existingTodo = await _context.TodoItems.FindAsync(todoItem.Id);
        if (existingTodo == null)
        {
            return null;
        }

        _context.Entry(existingTodo).CurrentValues.SetValues(todoItem);
        await _context.SaveChangesAsync();
        return existingTodo;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var todo = await _context.TodoItems.FindAsync(id);
        if (todo == null)
        {
            return false;
        }

        _context.TodoItems.Remove(todo);
        await _context.SaveChangesAsync();
        return true;
    }
}
