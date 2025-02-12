import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <div class="auth-container">
      <mat-card class="auth-card">
        <mat-card-header>
          <mat-card-title>Create Account</mat-card-title>
          <mat-card-subtitle>Join PitchSync and start tracking matches</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline">
              <mat-label>Email</mat-label>
              <input matInput type="email" formControlName="email" autocomplete="email" />
              @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
                <mat-error>Email is required</mat-error>
              }
              @if (form.get('email')?.hasError('email') && form.get('email')?.touched) {
                <mat-error>Enter a valid email</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Password</mat-label>
              <input matInput type="password" formControlName="password" autocomplete="new-password" />
              @if (form.get('password')?.hasError('required') && form.get('password')?.touched) {
                <mat-error>Password is required</mat-error>
              }
              @if (form.get('password')?.hasError('minlength') && form.get('password')?.touched) {
                <mat-error>At least 6 characters required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Display Name</mat-label>
              <input matInput formControlName="displayName" autocomplete="name" />
              @if (form.get('displayName')?.hasError('required') && form.get('displayName')?.touched) {
                <mat-error>Display name is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Favourite Team (optional)</mat-label>
              <input matInput formControlName="favoriteTeam" placeholder="e.g. Arsenal" />
            </mat-form-field>

            <mat-card-actions>
              <button mat-flat-button type="submit" [disabled]="form.invalid || loading">
                {{ loading ? 'Creating account…' : 'Create Account' }}
              </button>
              <button mat-button type="button" routerLink="/login">
                Already have an account?
              </button>
            </mat-card-actions>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    displayName: ['', [Validators.required]],
    favoriteTeam: [''],
  });

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { email, password, displayName, favoriteTeam } = this.form.getRawValue();
    this.auth.register(email!, password!, displayName!, favoriteTeam || undefined).subscribe({
      next: () => this.router.navigate(['/matches']),
      error: (err) => {
        this.loading = false;
        const msg = err?.error?.message ?? 'Registration failed. Please try again.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      }
    });
  }
}
