import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { PlayerLineupDto } from '../../models/match.model';

export interface LineupEditorDialogData {
  roomId: string;
  team: 'home' | 'away';
  teamLabel: string;
  players: PlayerLineupDto[];
}

interface DraftPlayer {
  playerName: string;
  shirtNumber: number | null;
  position: string;
  isStarting: boolean;
}

const POSITIONS = ['GK', 'CB', 'LB', 'RB', 'CDM', 'CM', 'CAM', 'LW', 'RW', 'ST'];

@Component({
  selector: 'app-lineup-editor-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatCheckboxModule, MatProgressSpinnerModule, MatDialogModule,
  ],
  template: `
    <h2 mat-dialog-title>Edit {{ data.teamLabel }} Lineup</h2>

    <mat-dialog-content>
      <div class="editor-list">
        @for (p of draftPlayers; track $index; let i = $index) {
          <div class="player-row">
            <input
              class="input shirt-input"
              type="number"
              [(ngModel)]="p.shirtNumber"
              placeholder="#"
              min="1" max="99"
            />
            <input
              class="input name-input"
              type="text"
              [(ngModel)]="p.playerName"
              placeholder="Player name"
            />
            <select class="input pos-select" [(ngModel)]="p.position">
              <option value="">Pos</option>
              @for (pos of positions; track pos) {
                <option [value]="pos">{{ pos }}</option>
              }
            </select>
            <label class="xi-toggle">
              <input type="checkbox" [(ngModel)]="p.isStarting" />
              XI
            </label>
            <button mat-icon-button class="remove-btn" (click)="removePlayer(i)"
                    aria-label="Remove player">
              <mat-icon>close</mat-icon>
            </button>
          </div>
        }
      </div>

      <button mat-stroked-button class="add-btn" (click)="addPlayer()">
        <mat-icon>add</mat-icon> Add Player
      </button>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="null">Cancel</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="saving">
        @if (saving) {
          <mat-spinner diameter="18" class="btn-spinner"></mat-spinner>
        } @else {
          Save
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content { min-width: 460px; max-height: 60vh; padding: 8px 24px; }
    .editor-list { display: flex; flex-direction: column; gap: 6px; margin-bottom: 12px; }
    .player-row {
      display: flex; align-items: center; gap: 6px;
      padding: 4px 0;
    }
    .input {
      border: 1px solid #ccc; border-radius: 4px;
      padding: 5px 8px; font-size: 13px; outline: none;
      background: white;
    }
    .input:focus { border-color: #1976d2; }
    .shirt-input { width: 52px; text-align: center; }
    .name-input { flex: 1; min-width: 100px; }
    .pos-select { width: 70px; }
    .xi-toggle {
      display: flex; align-items: center; gap: 4px;
      font-size: 13px; white-space: nowrap; cursor: pointer;
    }
    .remove-btn { color: rgba(0,0,0,.4); flex-shrink: 0; }
    .remove-btn:hover { color: #f44336; }
    .add-btn { margin-top: 4px; }
    .btn-spinner { display: inline-block; }
    mat-dialog-actions { padding: 8px 24px 16px; }
  `],
})
export class LineupEditorDialogComponent {
  readonly positions = POSITIONS;
  draftPlayers: DraftPlayer[];
  saving = false;

  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject<MatDialogRef<LineupEditorDialogComponent>>(MatDialogRef);

  constructor(@Inject(MAT_DIALOG_DATA) public data: LineupEditorDialogData) {
    this.draftPlayers = data.players.map(p => ({
      playerName: p.playerName,
      shirtNumber: p.shirtNumber ?? null,
      position: p.position ?? '',
      isStarting: p.isStarting,
    }));
  }

  addPlayer(): void {
    this.draftPlayers = [
      ...this.draftPlayers,
      { playerName: '', shirtNumber: null, position: '', isStarting: true },
    ];
  }

  removePlayer(index: number): void {
    this.draftPlayers = this.draftPlayers.filter((_, i) => i !== index);
  }

  async save(): Promise<void> {
    const valid = this.draftPlayers.filter(p => p.playerName.trim());
    if (!valid.length) {
      this.dialogRef.close(null);
      return;
    }
    this.saving = true;
    try {
      const payload: PlayerLineupDto[] = valid.map(p => ({
        playerName: p.playerName.trim(),
        shirtNumber: p.shirtNumber ?? undefined,
        position: p.position.trim() || undefined,
        isStarting: p.isStarting,
      }));
      const saved = await firstValueFrom(this.api.setLineup(this.data.roomId, this.data.team, payload));
      this.dialogRef.close(saved ?? payload);
    } catch {
      this.snackBar.open('Failed to save lineup', 'Dismiss', { duration: 3000 });
    } finally {
      this.saving = false;
    }
  }
}
