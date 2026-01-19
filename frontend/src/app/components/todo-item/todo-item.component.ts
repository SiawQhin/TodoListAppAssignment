import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TodoItem } from '../../models/todo-item.model';

@Component({
  selector: 'app-todo-item',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './todo-item.component.html',
  styleUrl: './todo-item.component.css'
})
export class TodoItemComponent {
  todo = input.required<TodoItem>();
  toggleComplete = output<string>();
  deleteTodo = output<string>();
  updateTodo = output<{ id: string; title: string }>();

  isEditing = signal(false);
  editedTitle = signal('');

  onToggleComplete() {
    this.toggleComplete.emit(this.todo().id);
  }

  onDelete() {
    this.deleteTodo.emit(this.todo().id);
  }

  startEditing() {
    this.isEditing.set(true);
    this.editedTitle.set(this.todo().title);
  }

  cancelEditing() {
    this.isEditing.set(false);
    this.editedTitle.set('');
  }

  saveEdit() {
    const trimmedTitle = this.editedTitle().trim();

    if (trimmedTitle && trimmedTitle !== this.todo().title) {
      this.updateTodo.emit({
        id: this.todo().id,
        title: trimmedTitle
      });
    }

    this.isEditing.set(false);
    this.editedTitle.set('');
  }
}
