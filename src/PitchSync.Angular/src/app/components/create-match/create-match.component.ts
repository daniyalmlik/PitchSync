import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OwlDateTimeModule, OwlNativeDateTimeModule } from '@danielmoncada/angular-datetime-picker';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-create-match',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatSlideToggleModule, MatIconModule,
    OwlDateTimeModule, OwlNativeDateTimeModule,
  ],
  template: `
    <div class="create-page">
      <mat-card class="create-card">
        <mat-card-header>
          <mat-card-title>Create Match Room</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="create-form">

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Room Title</mat-label>
              <input matInput formControlName="title" placeholder="e.g. Premier League" />
              @if (form.controls.title.invalid && form.controls.title.touched) {
                <mat-error>Title is required</mat-error>
              }
            </mat-form-field>

            <div class="teams-row">
              <mat-form-field appearance="outline" class="flex-field">
                <mat-label>Home Team</mat-label>
                <input matInput formControlName="homeTeam" />
                @if (form.controls.homeTeam.invalid && form.controls.homeTeam.touched) {
                  <mat-error>Required</mat-error>
                }
              </mat-form-field>
              <span class="vs">vs</span>
              <mat-form-field appearance="outline" class="flex-field">
                <mat-label>Away Team</mat-label>
                <input matInput formControlName="awayTeam" />
                @if (form.controls.awayTeam.invalid && form.controls.awayTeam.touched) {
                  <mat-error>Required</mat-error>
                }
              </mat-form-field>
            </div>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Competition (optional)</mat-label>
              <input matInput formControlName="competition" placeholder="e.g. Champions League" />
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Kick-off Date & Time</mat-label>
              <input matInput [owlDateTimeTrigger]="dtPicker"
                     [owlDateTime]="dtPicker"
                     formControlName="kickoffDateTime"
                     placeholder="Select date & time"
                     readonly />
              <mat-icon matIconSuffix [owlDateTimeTrigger]="dtPicker" style="cursor:pointer">event</mat-icon>
              @if (form.controls.kickoffDateTime.invalid && form.controls.kickoffDateTime.touched) {
                <mat-error>Kick-off date & time is required</mat-error>
              }
            </mat-form-field>
            <owl-date-time #dtPicker pickerType="both"></owl-date-time>

            <div class="toggle-row">
              <mat-slide-toggle formControlName="isPublic">
                {{ form.controls.isPublic.value ? 'Public room' : 'Private room (invite only)' }}
              </mat-slide-toggle>
            </div>

            <div class="actions">
              <button mat-stroked-button type="button" routerLink="/matches">Cancel</button>
              <button mat-flat-button color="primary" type="submit"
                      [disabled]="form.invalid || submitting">
                {{ submitting ? 'Creating...' : 'Create Room' }}
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .create-page {
      min-height: 100vh; display: flex; align-items: flex-start;
      justify-content: center; padding: 24px 16px;
      background: #f5f5f5;
    }
    .create-card { width: 100%; max-width: 560px; }
    .create-form { display: flex; flex-direction: column; gap: 4px; margin-top: 8px; }
    .full-width { width: 100%; }
    .teams-row { display: flex; align-items: center; gap: 8px; }
    .flex-field { flex: 1; }
    .vs { font-size: 18px; font-weight: 700; color: #555; flex-shrink: 0; }
    .toggle-row { padding: 8px 0; }
    .actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 8px; }
  `],
})

export class CreateMatchComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  submitting = false;

  form = this.fb.group({
    title: ['', Validators.required],
    homeTeam: ['', Validators.required],
    awayTeam: ['', Validators.required],
    competition: [''],
    kickoffDateTime: [null as Date | null, Validators.required],
    isPublic: [true],
  });

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.submitting) return;
    this.submitting = true;

    const v = this.form.getRawValue();
    const kickoffTime = (v.kickoffDateTime as Date).toISOString();
    

    try {
      const room = await this.api.createRoom({
        title: v.title!,
        homeTeam: v.homeTeam!,
        awayTeam: v.awayTeam!,
        competition: v.competition || undefined,
        kickoffTime,
        isPublic: v.isPublic!,
      }).toPromise();

      await this.router.navigate(['/matches', room!.id]);
    } catch {
      this.snackBar.open('Failed to create match room', 'Dismiss', { duration: 3000 });
    } finally {
      this.submitting = false;
    }
  }
}
