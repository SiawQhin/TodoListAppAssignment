import { Component, signal, inject, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = signal('');
  password = signal('');
  confirmPassword = signal('');
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  isLoading = signal(false);

  passwordsMatch = computed(() => {
    const pwd = this.password();
    const confirm = this.confirmPassword();
    return pwd === confirm;
  });

  canSubmit = computed(() => {
    return this.email() &&
           this.password() &&
           this.confirmPassword() &&
           this.passwordsMatch() &&
           !this.isLoading();
  });

  onRegister(): void {
    if (!this.passwordsMatch()) {
      this.error.set('Passwords do not match');
      return;
    }

    if (!this.email() || !this.password()) {
      this.error.set('Please fill in all fields');
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.authService.register(this.email(), this.password()).subscribe({
      next: () => {
        this.success.set('Registration successful! Redirecting to login...');
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err.error?.message) {
          this.error.set(err.error.message);
        } else if (err.error?.errors) {
          const errors = err.error.errors;
          const messages = Array.isArray(errors)
            ? errors.map((e: { description?: string }) => e.description).join('. ')
            : Object.values(errors).flat().join('. ');
          this.error.set(messages);
        } else {
          this.error.set('Registration failed. Please try again.');
        }
      }
    });
  }

  updateEmail(event: Event): void {
    this.email.set((event.target as HTMLInputElement).value);
  }

  updatePassword(event: Event): void {
    this.password.set((event.target as HTMLInputElement).value);
  }

  updateConfirmPassword(event: Event): void {
    this.confirmPassword.set((event.target as HTMLInputElement).value);
  }
}
