import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatchRoomResponse, MatchStatus, RoomRole } from '../../models/match.model';
import { SignalrService } from '../../services/signalr.service';

const STATUS_ORDER: MatchStatus[] = ['Upcoming', 'Live', 'HalfTime', 'SecondHalf', 'FullTime'];

@Component({
  selector: 'app-scoreboard',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatInputModule],
  template: `
    @if (room) {
      <div class="scoreboard">
        <div class="competition-line">
          @if (room.competition) { <span>{{ room.competition }}</span> · }
          <span>{{ room.kickoffTime | date:'EEE d MMM, HH:mm' }}</span>
          @if (showMinute) { · <span class="match-minute">{{ matchMinute }}'</span> }
        </div>

        <div class="score-area">
          <span class="team-name home-team">{{ room.homeTeam }}</span>

          @if (!editingScore) {
            <div class="score-display">
              <span class="score-value">{{ room.homeScore }}</span>
              <span class="score-sep">—</span>
              <span class="score-value">{{ room.awayScore }}</span>
            </div>
          } @else {
            <div class="score-edit">
              <input type="number" [(ngModel)]="editHome" min="0" class="score-input" />
              <span class="score-sep">—</span>
              <input type="number" [(ngModel)]="editAway" min="0" class="score-input" />
              <button mat-icon-button color="primary" (click)="saveScore()" [disabled]="saving">
                <mat-icon>check</mat-icon>
              </button>
              <button mat-icon-button (click)="editingScore = false">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          }

          <span class="team-name away-team">{{ room.awayTeam }}</span>
        </div>

        <div class="status-row">
          <span
            class="status-badge"
            [class]="'status-' + room.status.toLowerCase()"
            [class.clickable]="isHost"
            (click)="isHost && cycleStatus()">
            {{ statusLabel }}
          </span>

          @if (isHost && !editingScore) {
            <button mat-icon-button class="edit-score-btn" matTooltip="Edit score"
                    (click)="startEditScore()">
              <mat-icon>edit</mat-icon>
            </button>
          }
        </div>
      </div>
    } @else {
      <div class="scoreboard loading-placeholder">
        <div class="competition-line">&nbsp;</div>
        <div class="score-area">Loading match...</div>
      </div>
    }
  `,
  styles: [`
    .scoreboard {
      padding: 16px;
      text-align: center;
      background: #1a237e;
      color: white;
      border-radius: 8px;
    }
    .competition-line { font-size: 12px; opacity: 0.75; margin-bottom: 12px; }
    .match-minute { font-weight: 700; color: #80cbc4; }
    .score-area {
      display: flex; align-items: center; justify-content: center;
      gap: 16px; margin-bottom: 12px;
    }
    .team-name { font-size: 18px; font-weight: 600; flex: 1; }
    .home-team { text-align: right; }
    .away-team { text-align: left; }
    .score-display { display: flex; align-items: center; gap: 8px; }
    .score-value { font-size: 42px; font-weight: 800; min-width: 48px; text-align: center; }
    .score-sep { font-size: 32px; opacity: 0.7; }
    .score-edit { display: flex; align-items: center; gap: 4px; }
    .score-input {
      width: 52px; font-size: 32px; font-weight: 700;
      text-align: center; background: rgba(255,255,255,.15);
      border: none; border-bottom: 2px solid white; color: white;
      outline: none;
    }
    .status-row { display: flex; align-items: center; justify-content: center; gap: 8px; }
    .status-badge {
      display: inline-block; padding: 4px 12px; border-radius: 12px;
      font-size: 11px; font-weight: 700; letter-spacing: .8px; text-transform: uppercase;
    }
    .status-badge.clickable { cursor: pointer; }
    .status-upcoming { background: rgba(255,255,255,.2); }
    .status-live { background: #4caf50; animation: pulse-live 1.5s infinite; }
    .status-halftime { background: #ff9800; }
    .status-secondhalf { background: #4caf50; animation: pulse-live 1.5s infinite; }
    .status-fulltime { background: rgba(255,255,255,.2); }
    .status-abandoned { background: #f44336; }
    @keyframes pulse-live {
      0%, 100% { box-shadow: 0 0 0 0 rgba(76,175,80,.6); }
      50% { box-shadow: 0 0 0 6px rgba(76,175,80,0); }
    }
    .edit-score-btn { color: rgba(255,255,255,.7); }
    .loading-placeholder { min-height: 120px; display: flex; flex-direction: column; align-items: center; justify-content: center; opacity: .5; }
  `],
})
export class ScoreboardComponent {
  @Input() room: MatchRoomResponse | null = null;
  @Input() currentUserRole: RoomRole | null = null;

  private readonly signalr = inject(SignalrService);
  private readonly snackBar = inject(MatSnackBar);

  editingScore = false;
  editHome = 0;
  editAway = 0;
  saving = false;

  get isHost(): boolean { return this.currentUserRole === 'Host'; }

  get statusLabel(): string {
    switch (this.room?.status) {
      case 'Upcoming': return 'Upcoming';
      case 'Live': return 'Live';
      case 'HalfTime': return 'HT';
      case 'SecondHalf': return '2nd Half';
      case 'FullTime': return 'FT';
      case 'Abandoned': return 'Abandoned';
      default: return '';
    }
  }

  get showMinute(): boolean {
    return this.room?.status === 'Live' || this.room?.status === 'SecondHalf';
  }

  get matchMinute(): number {
    if (!this.room) return 0;
    const kickoff = new Date(this.room.kickoffTime).getTime();
    const elapsed = Math.floor((Date.now() - kickoff) / 60000);
    return Math.max(1, Math.min(elapsed, this.room.status === 'SecondHalf' ? 90 : 45));
  }

  startEditScore(): void {
    this.editHome = this.room?.homeScore ?? 0;
    this.editAway = this.room?.awayScore ?? 0;
    this.editingScore = true;
  }

  async saveScore(): Promise<void> {
    this.saving = true;
    try {
      await this.signalr.updateScore(this.editHome, this.editAway);
      this.editingScore = false;
    } catch {
      this.snackBar.open('Failed to update score', 'Dismiss', { duration: 3000 });
    } finally {
      this.saving = false;
    }
  }

  async cycleStatus(): Promise<void> {
    if (!this.room) return;
    const idx = STATUS_ORDER.indexOf(this.room.status);
    if (idx === -1 || idx >= STATUS_ORDER.length - 1) return;
    const next = STATUS_ORDER[idx + 1];
    try {
      await this.signalr.updateStatus(next);
    } catch {
      this.snackBar.open('Failed to update status', 'Dismiss', { duration: 3000 });
    }
  }
}
