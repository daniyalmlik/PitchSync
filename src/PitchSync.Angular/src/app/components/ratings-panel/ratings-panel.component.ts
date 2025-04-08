import { Component, Input, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSliderModule } from '@angular/material/slider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { SignalrService } from '../../services/signalr.service';
import { PlayerRatingResponse } from '../../models/rating.model';
import { RoomRole } from '../../models/match.model';

interface RateEvent {
  playerName: string;
  team: string;
  rating: number;
}

@Component({
  selector: 'app-ratings-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, MatTabsModule, MatSliderModule],
  template: `
    <mat-tab-group>
      <mat-tab [label]="homeTeam || 'Home'">
        <div class="ratings-tab">
          <ng-container *ngTemplateOutlet="playersBlock; context: { players: homePlayers }">
          </ng-container>
        </div>
      </mat-tab>
      <mat-tab [label]="awayTeam || 'Away'">
        <div class="ratings-tab">
          <ng-container *ngTemplateOutlet="playersBlock; context: { players: awayPlayers }">
          </ng-container>
        </div>
      </mat-tab>
    </mat-tab-group>

    <ng-template #playersBlock let-players="players">
      @if (players.length === 0) {
        <p class="empty-msg">No players rated yet</p>
      }
      @for (r of players; track r.playerName) {
        <div class="rating-row" [class.motm-row]="isMoTM(r)">
          <div class="name-cell">
            @if (isMoTM(r)) {
              <span class="motm-star" title="Man of the Match">⭐</span>
            }
            <span class="player-name">{{ r.playerName }}</span>
          </div>

          <div class="rating-cell">
            <span class="avg-badge" [class]="ratingClass(r.averageRating)">
              {{ r.averageRating | number:'1.1-1' }}
            </span>
            <span class="rating-count">{{ r.ratingCount }} ratings</span>

            @if (canRate) {
              <div class="slider-wrap">
                <mat-slider min="1" max="10" step="0.5" class="rating-slider">
                  <input
                    matSliderThumb
                    [ngModel]="sliderValue(r)"
                    (ngModelChange)="onSliderChange(r, $event)"
                  />
                </mat-slider>
                <span class="slider-label">{{ sliderValue(r) | number:'1.1-1' }}</span>
              </div>
            } @else if (r.myRating != null) {
              <span class="my-rating">Your rating: {{ r.myRating }}</span>
            }
          </div>
        </div>
      }
    </ng-template>
  `,
  styles: [`
    .ratings-tab { padding: 8px 4px; }
    .empty-msg { color: rgba(0,0,0,.4); font-size: 13px; margin: 12px 0; text-align: center; }
    .rating-row {
      display: flex; flex-direction: column; gap: 4px;
      padding: 8px 8px; border-radius: 6px;
      border-bottom: 1px solid #f0f0f0;
    }
    .motm-row { background: #fffde7; border-left: 3px solid #fbc02d; }
    .name-cell { display: flex; align-items: center; gap: 6px; }
    .motm-star { font-size: 14px; line-height: 1; }
    .player-name { font-size: 13px; font-weight: 600; }
    .rating-cell { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .avg-badge {
      min-width: 36px; height: 26px; border-radius: 13px;
      display: flex; align-items: center; justify-content: center;
      font-size: 13px; font-weight: 700; padding: 0 8px;
    }
    .rating-green { background: #c8e6c9; color: #1b5e20; }
    .rating-yellow { background: #fff9c4; color: #f57f17; }
    .rating-red { background: #ffcdd2; color: #b71c1c; }
    .rating-count { font-size: 11px; color: rgba(0,0,0,.45); }
    .slider-wrap { display: flex; align-items: center; gap: 4px; flex: 1; min-width: 140px; }
    .rating-slider { flex: 1; }
    .slider-label { font-size: 12px; font-weight: 600; min-width: 28px; color: #555; }
    .my-rating { font-size: 11px; color: rgba(0,0,0,.5); }
  `],
})
export class RatingsPanelComponent implements OnDestroy {
  @Input() ratings: PlayerRatingResponse[] = [];
  @Input() currentUserRole: RoomRole | null = null;
  @Input() homeTeam = '';
  @Input() awayTeam = '';

  private readonly signalr = inject(SignalrService);
  private readonly snackBar = inject(MatSnackBar);

  private readonly rateSubject = new Subject<RateEvent>();
  private readonly rateSub: Subscription;

  draftRatings: Record<string, number> = {};

  constructor() {
    this.rateSub = this.rateSubject.pipe(
      debounceTime(500),
      switchMap(({ playerName, team, rating }) =>
        this.signalr.ratePlayer(playerName, team, rating)
      ),
    ).subscribe({
      error: () => this.snackBar.open('Failed to submit rating', 'Dismiss', { duration: 3000 }),
    });
  }

  ngOnDestroy(): void {
    this.rateSub.unsubscribe();
  }

  get canRate(): boolean {
    return this.currentUserRole === 'Spectator' || this.currentUserRole === 'Commentator';
  }

  get homePlayers(): PlayerRatingResponse[] {
    return this.ratings
      .filter(r => r.team.toLowerCase() === this.homeTeam.toLowerCase() || r.team === 'home')
      .slice()
      .sort((a, b) => b.averageRating - a.averageRating);
  }

  get awayPlayers(): PlayerRatingResponse[] {
    return this.ratings
      .filter(r => r.team.toLowerCase() === this.awayTeam.toLowerCase() || r.team === 'away')
      .slice()
      .sort((a, b) => b.averageRating - a.averageRating);
  }

  get motmPlayer(): PlayerRatingResponse | null {
    const allRated = this.ratings.filter(r => r.ratingCount > 0);
    if (!allRated.length) return null;
    return allRated.reduce((best, r) => r.averageRating > best.averageRating ? r : best);
  }

  isMoTM(r: PlayerRatingResponse): boolean {
    const motm = this.motmPlayer;
    return motm !== null && motm.playerName === r.playerName && motm.team === r.team;
  }

  ratingClass(avg: number): string {
    if (avg >= 7) return 'avg-badge rating-green';
    if (avg >= 5) return 'avg-badge rating-yellow';
    return 'avg-badge rating-red';
  }

  sliderValue(r: PlayerRatingResponse): number {
    const key = r.playerName + '|' + r.team;
    return this.draftRatings[key] ?? r.myRating ?? 5;
  }

  onSliderChange(r: PlayerRatingResponse, value: number): void {
    const key = r.playerName + '|' + r.team;
    this.draftRatings = { ...this.draftRatings, [key]: value };
    this.rateSubject.next({ playerName: r.playerName, team: r.team, rating: value });
  }
}
