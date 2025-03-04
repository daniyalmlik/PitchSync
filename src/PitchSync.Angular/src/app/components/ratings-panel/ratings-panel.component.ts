import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SignalrService } from '../../services/signalr.service';
import { PlayerRatingResponse } from '../../models/rating.model';
import { RoomRole } from '../../models/match.model';

@Component({
  selector: 'app-ratings-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatInputModule, MatFormFieldModule],
  template: `
    <div class="ratings-panel">
      @if (ratings.length === 0) {
        <p class="empty-msg">No ratings yet</p>
      } @else {
        @for (group of teamGroups; track group.team) {
          <div class="team-section">
            <div class="team-label">{{ group.team }}</div>
            @for (r of group.players; track r.playerName) {
              <div class="rating-row">
                <span class="player-name">{{ r.playerName }}</span>
                <div class="rating-right">
                  <span class="avg-rating" [class]="ratingClass(r.averageRating)">
                    {{ r.averageRating | number:'1.1-1' }}
                  </span>
                  <span class="rating-count">({{ r.ratingCount }})</span>

                  @if (canRate) {
                    <input
                      type="number" min="1" max="10"
                      class="rating-input"
                      [(ngModel)]="draftRatings[r.playerName + '|' + r.team]"
                      placeholder="{{ r.myRating ?? '–' }}"
                    />
                    <button mat-stroked-button class="rate-btn"
                            (click)="submitRating(r)"
                            [disabled]="!isValidRating(r.playerName, r.team)">
                      Rate
                    </button>
                  } @else if (r.myRating != null) {
                    <span class="my-rating">Yours: {{ r.myRating }}</span>
                  }
                </div>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .ratings-panel { padding: 4px 0; }
    .empty-msg { color: rgba(0,0,0,.4); font-size: 13px; margin: 8px 0; }
    .team-section { margin-bottom: 12px; }
    .team-label {
      font-size: 11px; font-weight: 700; letter-spacing: .6px;
      text-transform: uppercase; color: #1976d2; margin-bottom: 4px;
    }
    .rating-row {
      display: flex; align-items: center; justify-content: space-between;
      padding: 6px 4px; border-bottom: 1px solid #f0f0f0;
    }
    .player-name { font-size: 13px; font-weight: 500; flex: 1; }
    .rating-right { display: flex; align-items: center; gap: 6px; }
    .avg-rating {
      font-size: 15px; font-weight: 700; min-width: 32px; text-align: center;
    }
    .rating-green { color: #2e7d32; }
    .rating-yellow { color: #f57f17; }
    .rating-red { color: #c62828; }
    .rating-count { font-size: 11px; color: rgba(0,0,0,.45); }
    .my-rating { font-size: 11px; color: rgba(0,0,0,.5); }
    .rating-input {
      width: 44px; border: 1px solid #ccc; border-radius: 4px;
      padding: 3px 6px; font-size: 13px; text-align: center; outline: none;
    }
    .rating-input:focus { border-color: #1976d2; }
    .rate-btn { min-width: 48px; height: 28px; font-size: 11px; line-height: 28px; padding: 0 8px; }
  `],
})
export class RatingsPanelComponent {
  @Input() ratings: PlayerRatingResponse[] = [];
  @Input() currentUserRole: RoomRole | null = null;
  @Input() homeTeam = '';
  @Input() awayTeam = '';

  private readonly signalr = inject(SignalrService);
  private readonly snackBar = inject(MatSnackBar);

  draftRatings: Record<string, number | null> = {};

  get canRate(): boolean {
    return this.currentUserRole === 'Spectator' || this.currentUserRole === 'Commentator';
  }

  get teamGroups(): { team: string; players: PlayerRatingResponse[] }[] {
    const teams = [...new Set(this.ratings.map(r => r.team))];
    return teams.map(team => ({
      team,
      players: this.ratings.filter(r => r.team === team),
    }));
  }

  ratingClass(avg: number): string {
    if (avg >= 7) return 'rating-green';
    if (avg >= 5) return 'rating-yellow';
    return 'rating-red';
  }

  isValidRating(playerName: string, team: string): boolean {
    const val = this.draftRatings[playerName + '|' + team];
    return val != null && val >= 1 && val <= 10;
  }

  async submitRating(r: PlayerRatingResponse): Promise<void> {
    const key = r.playerName + '|' + r.team;
    const rating = this.draftRatings[key];
    if (rating == null || rating < 1 || rating > 10) return;

    try {
      await this.signalr.ratePlayer(r.playerName, r.team, rating);
      delete this.draftRatings[key];
    } catch {
      this.snackBar.open('Failed to submit rating', 'Dismiss', { duration: 3000 });
    }
  }
}
