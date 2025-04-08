import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { PlayerLineupDto, RoomRole } from '../../models/match.model';
import { MatchStateService } from '../../services/match-state.service';
import {
  LineupEditorDialogComponent,
  LineupEditorDialogData,
} from '../lineup-editor-dialog/lineup-editor-dialog.component';

@Component({
  selector: 'app-lineup-panel',
  standalone: true,
  imports: [
    CommonModule, MatTabsModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatDialogModule,
  ],
  template: `
    <mat-tab-group>
      <mat-tab [label]="homeTeam || 'Home'">
        <div class="lineup-tab">
          <ng-container *ngTemplateOutlet="lineupContent; context: { players: homeLineup, team: 'home' }">
          </ng-container>
        </div>
      </mat-tab>
      <mat-tab [label]="awayTeam || 'Away'">
        <div class="lineup-tab">
          <ng-container *ngTemplateOutlet="lineupContent; context: { players: awayLineup, team: 'away' }">
          </ng-container>
        </div>
      </mat-tab>
    </mat-tab-group>

    <ng-template #lineupContent let-players="players" let-team="team">
      @if (canEdit) {
        <div class="edit-row">
          <button mat-stroked-button color="primary" class="edit-btn"
                  (click)="openEditor(team)">
            <mat-icon>edit</mat-icon> Edit Lineup
          </button>
        </div>
      }

      @if (starters(players).length > 0) {
        <div class="section-label">Starting XI</div>
        @for (p of starters(players); track p.playerName) {
          <div class="player-row">
            <span class="shirt-badge">{{ p.shirtNumber ?? '–' }}</span>
            <span class="player-name">{{ p.playerName }}</span>
            @if (p.position) {
              <span class="pos-chip">{{ p.position }}</span>
            }
          </div>
        }
      }

      @if (subs(players).length > 0) {
        <div class="section-label subs-label">Subs</div>
        @for (p of subs(players); track p.playerName) {
          <div class="player-row sub-row">
            <span class="shirt-badge shirt-sub">{{ p.shirtNumber ?? '–' }}</span>
            <span class="player-name">{{ p.playerName }}</span>
            @if (p.position) {
              <span class="pos-chip">{{ p.position }}</span>
            }
          </div>
        }
      }

      @if (players.length === 0) {
        <p class="empty-msg">No lineup set yet</p>
      }
    </ng-template>
  `,
  styles: [`
    .lineup-tab { padding: 10px 4px; }
    .edit-row { display: flex; justify-content: flex-end; margin-bottom: 8px; }
    .edit-btn { height: 32px; font-size: 12px; }
    .section-label {
      font-size: 10px; font-weight: 700; letter-spacing: .6px;
      text-transform: uppercase; color: #1976d2;
      margin: 8px 0 4px; padding: 0 4px;
    }
    .subs-label { color: #757575; }
    .player-row {
      display: flex; align-items: center; gap: 8px;
      padding: 5px 8px; border-radius: 4px;
      background: #f5f5f5; margin-bottom: 3px;
    }
    .sub-row { background: #fafafa; opacity: .85; }
    .shirt-badge {
      min-width: 26px; height: 26px; border-radius: 50%;
      background: #1976d2; color: white;
      display: flex; align-items: center; justify-content: center;
      font-size: 11px; font-weight: 700; flex-shrink: 0;
    }
    .shirt-sub { background: #9e9e9e; }
    .player-name { flex: 1; font-size: 13px; font-weight: 500; }
    .pos-chip {
      font-size: 10px; padding: 2px 7px; border-radius: 10px;
      background: #e3f2fd; color: #1565c0; white-space: nowrap;
    }
    .empty-msg { color: rgba(0,0,0,.4); font-size: 13px; margin: 12px 0; text-align: center; }
  `],
})
export class LineupPanelComponent {
  @Input() homeLineup: PlayerLineupDto[] = [];
  @Input() awayLineup: PlayerLineupDto[] = [];
  @Input() homeTeam = '';
  @Input() awayTeam = '';
  @Input() currentUserRole: RoomRole | null = null;
  @Input() roomId = '';

  private readonly dialog = inject(MatDialog);
  private readonly matchState = inject(MatchStateService);

  get canEdit(): boolean {
    return this.currentUserRole === 'Host' || this.currentUserRole === 'Commentator';
  }

  starters(players: PlayerLineupDto[]): PlayerLineupDto[] {
    return players.filter(p => p.isStarting);
  }

  subs(players: PlayerLineupDto[]): PlayerLineupDto[] {
    return players.filter(p => !p.isStarting);
  }

  openEditor(team: 'home' | 'away'): void {
    const players = team === 'home' ? this.homeLineup : this.awayLineup;
    const teamLabel = team === 'home' ? (this.homeTeam || 'Home') : (this.awayTeam || 'Away');

    const data: LineupEditorDialogData = { roomId: this.roomId, team, teamLabel, players };
    const ref = this.dialog.open(LineupEditorDialogComponent, {
      data,
      width: '540px',
      maxHeight: '90vh',
    });

    ref.afterClosed().subscribe((saved: PlayerLineupDto[] | null) => {
      if (saved) {
        this.matchState.updateLineup(team, saved);
      }
    });
  }
}
