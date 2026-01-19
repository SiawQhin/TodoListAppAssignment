import { Component, signal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-todo-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './todo-form.component.html',
  styleUrl: './todo-form.component.css'
})
export class TodoFormComponent {
  title = signal('');
  todoAdded = output<string>();

  onSubmit() {
    const trimmedTitle = this.title().trim();

    if (trimmedTitle) {
      this.todoAdded.emit(trimmedTitle);
      this.title.set('');
    }
  }
}
