import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TodoFormComponent } from '../todo-form/todo-form.component';
import { TodoItemComponent } from '../todo-item/todo-item.component';
import { TodoService } from '../../services/todo.service';
import { AuthService } from '../../services/auth.service';
import { TodoItem } from '../../models/todo-item.model';

@Component({
  selector: 'app-todo-list',
  standalone: true,
  imports: [TodoFormComponent, TodoItemComponent],
  templateUrl: './todo-list.component.html',
  styleUrl: './todo-list.component.css'
})
export class TodoListComponent implements OnInit {
  private todoService = inject(TodoService);
  private authService = inject(AuthService);
  private router = inject(Router);

  todos = signal<TodoItem[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);
  userEmail = signal<string | null>(null);

  ngOnInit() {
    this.userEmail.set(this.authService.getEmail());
    this.loadTodos();
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  loadTodos() {
    this.isLoading.set(true);
    this.error.set(null);

    this.todoService.getAll().subscribe({
      next: (todos) => {
        this.todos.set(todos);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load todos. Please try again.');
        this.isLoading.set(false);
        console.error('Error loading todos:', err);
      }
    });
  }

  onTodoAdded(title: string) {
    this.todoService.create({ title }).subscribe({
      next: (newTodo) => {
        this.todos.update(todos => [...todos, newTodo]);
      },
      error: (err) => {
        this.error.set('Failed to add todo. Please try again.');
        console.error('Error adding todo:', err);
      }
    });
  }

  onToggleComplete(id: string) {
    const todo = this.todos().find(t => t.id === id);
    if (!todo) return;

    this.todoService.update(id, {
      title: todo.title,
      isCompleted: !todo.isCompleted
    }).subscribe({
      next: (updatedTodo) => {
        this.todos.update(todos =>
          todos.map(t => t.id === id ? updatedTodo : t)
        );
      },
      error: (err) => {
        this.error.set('Failed to update todo. Please try again.');
        console.error('Error updating todo:', err);
      }
    });
  }

  onUpdateTodo(event: { id: string; title: string }) {
    const todo = this.todos().find(t => t.id === event.id);
    if (!todo) return;

    this.todoService.update(event.id, {
      title: event.title,
      isCompleted: todo.isCompleted
    }).subscribe({
      next: (updatedTodo) => {
        this.todos.update(todos =>
          todos.map(t => t.id === event.id ? updatedTodo : t)
        );
      },
      error: (err) => {
        this.error.set('Failed to update todo. Please try again.');
        console.error('Error updating todo:', err);
      }
    });
  }

  onDeleteTodo(id: string) {
    this.todoService.delete(id).subscribe({
      next: () => {
        this.todos.update(todos => todos.filter(t => t.id !== id));
      },
      error: (err) => {
        this.error.set('Failed to delete todo. Please try again.');
        console.error('Error deleting todo:', err);
      }
    });
  }
}
