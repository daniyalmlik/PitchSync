import { Component, Input, inject, OnInit } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSnackBar } from '@angular/material/snack-bar';
import { map, startWith } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { MatchStateService } from '../../services/match-state.service';
import { MatchEventType, PostEventRequest } from '../../models/event.model';
import { PlayerLineupDto } from '../../models/match.model';

const EVENT_TYPES: { value: MatchEventType; label: string }[] = [
  { value: 'Goal', label: 'Goal' },
  { value: 'OwnGoal', label: 'Own Goal' },
  { value: 'Assist', label: 'Assist' },
  { value: 'YellowCard', label: 'Yellow Card' },
  { value: 'RedCard', label: 'Red Card' },
  { value: 'Substitution', label: 'Substitution' },
  { value: 'Penalty', label: 'Penalty' },
  { value: 'PenaltyMiss', label: 'Penalty Miss' },
  { value: 'VAR', label: 'VAR' },
  { value: 'Injury', label: 'Injury' },
  { value: 'HalfTime', label: 'Half Time' },
  { value: 'FullTime', label: 'Full Time' },
  { value: 'KickOff', label: 'Kick Off' },
  { value: 'FreeKick', label: 'Free Kick' },
  { value: 'Corner', label: 'Corner' },
  { value: 'Save', label: 'Save' },
  { value: 'Comment', label: 'Comment' },
];

@Component({
  selector: 'app-event-form',
  standalone: true,
  imports: [
    CommonModule, AsyncPipe, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonToggleModule, MatButtonModule, MatAutocompleteModule,
  ],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="event-form">
      <div class="form-row">
        <mat-form-field class="field-minute" appearance="outline">
          <mat-label>Min</mat-label>
          <input matInput type="number" formControlName="minute" min="0" max="120" />
        </mat-form-field>

        <mat-form-field class="field-type" appearance="outline">
          <mat-label>Event</mat-label>
          <mat-select formControlName="eventType">
            @for (et of eventTypes; track et.value) {
              <mat-option [value]="et.value">{{ et.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-button-toggle-group formControlName="team" class="team-toggle">
          <mat-button-toggle value="home">Home</mat-button-toggle>
          <mat-button-toggle value="away">Away</mat-button-toggle>
        </mat-button-toggle-group>

        <mat-form-field class="field-player" appearance="outline">
          <mat-label>Player</mat-label>
          <input matInput formControlName="playerName"
                 [matAutocomplete]="autoPlayer" placeholder="Player name" />
          <mat-autocomplete #autoPlayer="matAutocomplete">
            @for (name of filteredPlayers$ | async; track name) {
              <mat-option [value]="name">{{ name }}</mat-option>
            }
          </mat-autocomplete>
        </mat-form-field>

        @if (isSubstitution) {
          <mat-form-field class="field-player" appearance="outline">
            <mat-label>Sub Off</mat-label>
            <input matInput formControlName="secondaryPlayerName"
                   [matAutocomplete]="autoSub" placeholder="Player off" />
            <mat-autocomplete #autoSub="matAutocomplete">
              @for (name of filteredPlayers$ | async; track name) {
                <mat-option [value]="name">{{ name }}</mat-option>
              }
            </mat-autocomplete>
          </mat-form-field>
        }

        <mat-form-field class="field-desc" appearance="outline">
          <mat-label>Note</mat-label>
          <input matInput formControlName="description" placeholder="Optional note" />
        </mat-form-field>

        <button mat-flat-button color="primary" type="submit"
                [disabled]="form.invalid || submitting" class="submit-btn">
          {{ submitting ? '...' : 'Post' }}
        </button>
      </div>
    </form>
  `,
  styles: [`
    .event-form { padding: 8px 0; }
    .form-row {
      display: flex; flex-wrap: wrap; align-items: center; gap: 8px;
    }
    .field-minute { width: 72px; }
    .field-type { width: 140px; }
    .field-player { width: 150px; }
    .field-desc { flex: 1; min-width: 120px; }
    .team-toggle { height: 40px; }
    .submit-btn { height: 40px; flex-shrink: 0; }
    mat-form-field { margin-bottom: -1.25em; }
    @media (max-width: 600px) {
      .form-row { flex-direction: column; align-items: stretch; }
      .field-minute, .field-type, .field-player, .field-desc { width: 100%; }
      .team-toggle { width: 100%; }
      .submit-btn { width: 100%; }
    }
  `],
})
export class EventFormComponent implements OnInit {
  @Input() homeLineup: PlayerLineupDto[] = [];
  @Input() awayLineup: PlayerLineupDto[] = [];
  @Input() homeTeam = '';
  @Input() awayTeam = '';

  private readonly matchState = inject(MatchStateService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly eventTypes = EVENT_TYPES;
  submitting = false;

  form = this.fb.group({
    minute: [1, [Validators.required, Validators.min(0), Validators.max(120)]],
    eventType: ['Goal' as MatchEventType, Validators.required],
    team: ['home'],
    playerName: [''],
    secondaryPlayerName: [''],
    description: [''],
  });

  filteredPlayers$!: Observable<string[]>;

  ngOnInit(): void {
    this.filteredPlayers$ = this.form.controls.playerName.valueChanges.pipe(
      startWith(''),
      map(val => this.filterPlayers(val ?? '')),
    );
  }

  get isSubstitution(): boolean {
    return this.form.controls.eventType.value === 'Substitution';
  }

  private get activeLineup(): PlayerLineupDto[] {
    return this.form.controls.team.value === 'home' ? this.homeLineup : this.awayLineup;
  }

  private filterPlayers(val: string): string[] {
    const lower = val.toLowerCase();
    return this.activeLineup
      .map(p => p.playerName)
      .filter(name => name.toLowerCase().includes(lower));
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.submitting) return;

    const v = this.form.getRawValue();
    const prevMinute = v.minute ?? 1;

    const request: PostEventRequest = {
      minute: v.minute!,
      eventType: v.eventType as MatchEventType,
      team: v.team === 'home' ? this.homeTeam : v.team === 'away' ? this.awayTeam : undefined,
      playerName: v.playerName || undefined,
      secondaryPlayerName: this.isSubstitution ? (v.secondaryPlayerName || undefined) : undefined,
      description: v.description || undefined,
    };

    this.submitting = true;
    try {
      await this.matchState.postEventOptimistic(request);
      this.form.reset({
        minute: Math.min(prevMinute + 1, 120),
        eventType: 'Goal',
        team: 'home',
        playerName: '',
        secondaryPlayerName: '',
        description: '',
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to post event';
      this.snackBar.open(msg, 'Dismiss', { duration: 3000 });
    } finally {
      this.submitting = false;
    }
  }
}
