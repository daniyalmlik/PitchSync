import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { PlayerLineupDto, RoomRole } from '../../models/match.model';

interface DraftPlayer {
  playerName: string;
  shirtNumber: number | null;
  position: string;
  isStarting: boolean;
}

@Component({
  selector: 'app-lineup-panel',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatTabsModule, MatButtonModule,
    MatIconModule, MatFormFieldModule, MatInputModule, MatChipsModule,
  ],
  template: `
    <mat-tab-group>
      <mat-tab [label]="homeTeam || 'Home'">
        <div class="lineup-tab">
          <div class="player-list">
            @for (p of homeLineup; track p.playerName) {
              <div class="player-row">
                @if (p.shirtNumber != null) {
                  <span class="shirt-num">{{ p.shirtNumber }}</span>
                }
                <span class="player-name">{{ p.playerName }}</span>
                @if (p.position) {
                  <span class="position">{{ p.position }}</span>
                }
                <span class="status-chip" [class.chip-starting]="p.isStarting">
                  {{ p.isStarting ? 'Starting' : 'Sub' }}
                </span>
              </div>
            }
            @if (homeLineup.length === 0) {
              <p class="empty-msg">No players added yet</p>
            }
          </div>

          @if (canEdit) {
            <div class="add-player-form">
              <h4>Add Player</h4>
              <div class="add-row">
                <input class="small-input" type="number" [(ngModel)]="homeForm.shirtNumber"
                       placeholder="#" style="width:48px" />
                <input class="small-input flex-grow" [(ngModel)]="homeForm.playerName"
                       placeholder="Player name" />
                <input class="small-input" [(ngModel)]="homeForm.position"
                       placeholder="Pos" style="width:60px" />
                <label class="toggle-label">
                  <input type="checkbox" [(ngModel)]="homeForm.isStarting" />
                  XI
                </label>
                <button mat-icon-button color="primary" (click)="addPlayer('home')"
                        [disabled]="!homeForm.playerName.trim()">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
              <button mat-stroked-button color="primary" class="save-btn"
                      (click)="saveLineup('home')" [disabled]="saving">
                Save Lineup
              </button>
            </div>
          }
        </div>
      </mat-tab>

      <mat-tab [label]="awayTeam || 'Away'">
        <div class="lineup-tab">
          <div class="player-list">
            @for (p of awayLineup; track p.playerName) {
              <div class="player-row">
                @if (p.shirtNumber != null) {
                  <span class="shirt-num">{{ p.shirtNumber }}</span>
                }
                <span class="player-name">{{ p.playerName }}</span>
                @if (p.position) {
                  <span class="position">{{ p.position }}</span>
                }
                <span class="status-chip" [class.chip-starting]="p.isStarting">
                  {{ p.isStarting ? 'Starting' : 'Sub' }}
                </span>
              </div>
            }
            @if (awayLineup.length === 0) {
              <p class="empty-msg">No players added yet</p>
            }
          </div>

          @if (canEdit) {
            <div class="add-player-form">
              <h4>Add Player</h4>
              <div class="add-row">
                <input class="small-input" type="number" [(ngModel)]="awayForm.shirtNumber"
                       placeholder="#" style="width:48px" />
                <input class="small-input flex-grow" [(ngModel)]="awayForm.playerName"
                       placeholder="Player name" />
                <input class="small-input" [(ngModel)]="awayForm.position"
                       placeholder="Pos" style="width:60px" />
                <label class="toggle-label">
                  <input type="checkbox" [(ngModel)]="awayForm.isStarting" />
                  XI
                </label>
                <button mat-icon-button color="primary" (click)="addPlayer('away')"
                        [disabled]="!awayForm.playerName.trim()">
                  <mat-icon>add</mat-icon>
                </button>
              </div>
              <button mat-stroked-button color="primary" class="save-btn"
                      (click)="saveLineup('away')" [disabled]="saving">
                Save Lineup
              </button>
            </div>
          }
        </div>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [`
    .lineup-tab { padding: 12px 4px; }
    .player-list { display: flex; flex-direction: column; gap: 4px; min-height: 40px; }
    .player-row {
      display: flex; align-items: center; gap: 8px;
      padding: 6px 8px; border-radius: 4px; background: #f5f5f5;
    }
    .shirt-num {
      min-width: 24px; text-align: center; font-weight: 700;
      font-size: 12px; color: #555;
    }
    .player-name { flex: 1; font-size: 14px; font-weight: 500; }
    .position { font-size: 11px; color: #888; }
    .status-chip {
      font-size: 10px; padding: 2px 7px; border-radius: 10px;
      background: #e0e0e0; color: #555; white-space: nowrap;
    }
    .chip-starting { background: #c8e6c9; color: #1b5e20; }
    .empty-msg { color: rgba(0,0,0,.4); font-size: 13px; margin: 8px 0; }
    .add-player-form { margin-top: 12px; }
    .add-player-form h4 { margin: 0 0 8px; font-size: 13px; font-weight: 600; color: #555; }
    .add-row { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .small-input {
      border: 1px solid #ccc; border-radius: 4px; padding: 5px 8px;
      font-size: 13px; outline: none;
    }
    .small-input:focus { border-color: #1976d2; }
    .flex-grow { flex: 1; min-width: 100px; }
    .toggle-label { display: flex; align-items: center; gap: 4px; font-size: 13px; cursor: pointer; }
    .save-btn { margin-top: 8px; }
  `],
})
export class LineupPanelComponent {
  @Input() homeLineup: PlayerLineupDto[] = [];
  @Input() awayLineup: PlayerLineupDto[] = [];
  @Input() homeTeam = '';
  @Input() awayTeam = '';
  @Input() currentUserRole: RoomRole | null = null;
  @Input() roomId = '';

  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);

  saving = false;

  homeForm: DraftPlayer = { playerName: '', shirtNumber: null, position: '', isStarting: true };
  awayForm: DraftPlayer = { playerName: '', shirtNumber: null, position: '', isStarting: true };

  get canEdit(): boolean {
    return this.currentUserRole === 'Host' || this.currentUserRole === 'Commentator';
  }

  addPlayer(team: 'home' | 'away'): void {
    const form = team === 'home' ? this.homeForm : this.awayForm;
    if (!form.playerName.trim()) return;

    const player: PlayerLineupDto = {
      playerName: form.playerName.trim(),
      shirtNumber: form.shirtNumber ?? undefined,
      position: form.position.trim() || undefined,
      isStarting: form.isStarting,
    };

    if (team === 'home') {
      this.homeLineup = [...this.homeLineup, player];
      this.homeForm = { playerName: '', shirtNumber: null, position: '', isStarting: true };
    } else {
      this.awayLineup = [...this.awayLineup, player];
      this.awayForm = { playerName: '', shirtNumber: null, position: '', isStarting: true };
    }
  }

  async saveLineup(team: 'home' | 'away'): Promise<void> {
    if (!this.roomId) return;
    this.saving = true;
    const players = team === 'home' ? this.homeLineup : this.awayLineup;
    const teamName = team === 'home' ? this.homeTeam : this.awayTeam;
    try {
      await this.api.setLineup(this.roomId, teamName, players).toPromise();
      this.snackBar.open('Lineup saved', '', { duration: 2000 });
    } catch {
      this.snackBar.open('Failed to save lineup', 'Dismiss', { duration: 3000 });
    } finally {
      this.saving = false;
    }
  }
}
