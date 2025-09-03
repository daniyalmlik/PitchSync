import { Component, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../services/api.service';
import { InvitesService } from '../../services/invites.service';
import { RoomInviteDto } from '../../models/match.model';

@Component({
  selector: 'app-pending-invites-dialog',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Pending Invites</h2>

    <mat-dialog-content>
      @if (invites.pendingInvites$ | async; as list) {
        @if (list.length === 0) {
          <p class="empty-msg">No pending invites</p>
        } @else {
          <div class="invite-list">
            @for (invite of list; track invite.id) {
              <div class="invite-row">
                <div class="invite-info">
                  <span class="room-title">{{ invite.roomTitle }}</span>
                  <span class="teams">{{ invite.homeTeam }} vs {{ invite.awayTeam }}</span>
                  <span class="invited-by">Invited by {{ invite.invitedByDisplayName }}</span>
                </div>
                <div class="invite-actions">
                  <button mat-flat-button color="primary" class="action-btn"
                          [disabled]="processing.has(invite.id)"
                          (click)="accept(invite)">
                    @if (processing.has(invite.id)) {
                      <mat-spinner diameter="14"></mat-spinner>
                    } @else {
                      Accept
                    }
                  </button>
                  <button mat-stroked-button class="action-btn"
                          [disabled]="processing.has(invite.id)"
                          (click)="decline(invite)">
                    Decline
                  </button>
                </div>
              </div>
            }
          </div>
        }
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content { min-width: 380px; max-height: 60vh; padding: 8px 24px; }
    .empty-msg { text-align: center; color: rgba(0,0,0,.4); font-size: 14px; margin: 24px 0; }
    .invite-list { display: flex; flex-direction: column; gap: 8px; }
    .invite-row {
      display: flex; align-items: center; gap: 12px;
      padding: 12px; border-radius: 8px; background: #f5f5f5;
    }
    .invite-info { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .room-title { font-size: 14px; font-weight: 600; }
    .teams { font-size: 13px; color: #424242; }
    .invited-by { font-size: 12px; color: #757575; }
    .invite-actions { display: flex; gap: 6px; flex-shrink: 0; }
    .action-btn { height: 32px; font-size: 12px; min-width: 72px; }
    mat-dialog-actions { padding: 8px 24px 16px; }
  `],
})
export class PendingInvitesDialogComponent {
  readonly invites = inject(InvitesService);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly dialogRef = inject(MatDialogRef<PendingInvitesDialogComponent>);

  processing = new Set<string>();

  accept(invite: RoomInviteDto): void {
    this.processing.add(invite.id);
    this.api.acceptInvite(invite.id).subscribe({
      next: result => {
        this.invites.removeInvite(invite.id);
        this.processing.delete(invite.id);
        this.dialogRef.close();
        this.router.navigate(['/matches', result.matchRoomId]);
      },
      error: () => {
        this.processing.delete(invite.id);
      },
    });
  }

  decline(invite: RoomInviteDto): void {
    this.processing.add(invite.id);
    this.api.declineInvite(invite.id).subscribe({
      next: () => {
        this.invites.removeInvite(invite.id);
        this.processing.delete(invite.id);
      },
      error: () => {
        this.processing.delete(invite.id);
      },
    });
  }
}
