import { Component, Inject, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, catchError, takeUntil } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { UserInfo } from '../../models/user.model';
import { RoomInviteDto } from '../../models/match.model';

export interface InviteUsersDialogData {
  roomId: string;
  existingParticipantIds: string[];
}

@Component({
  selector: 'app-invite-users-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatDialogModule,
  ],
  template: `
    <h2 mat-dialog-title>Invite Players</h2>

    <mat-dialog-content>
      <div class="search-row">
        <input
          class="search-input"
          type="text"
          placeholder="Search by name or email..."
          [(ngModel)]="query"
          (ngModelChange)="onQueryChange($event)"
          autocomplete="off"
        />
        @if (searching) {
          <mat-spinner diameter="18" class="search-spinner"></mat-spinner>
        }
      </div>

      @if (results.length > 0) {
        <div class="results-list">
          @for (user of results; track user.id) {
            <div class="user-row">
              <div class="user-info">
                <span class="user-name">{{ user.displayName }}</span>
                <span class="user-email">{{ user.email }}</span>
              </div>
              @if (invited.has(user.id)) {
                <span class="invited-badge">
                  <mat-icon>check_circle</mat-icon> Invited
                </span>
              } @else if (data.existingParticipantIds.includes(user.id)) {
                <span class="already-badge">Already joined</span>
              } @else {
                <button mat-stroked-button color="primary" class="invite-btn"
                        [disabled]="inviting.has(user.id)"
                        (click)="invite(user)">
                  @if (inviting.has(user.id)) {
                    <mat-spinner diameter="14"></mat-spinner>
                  } @else {
                    Invite
                  }
                </button>
              }
            </div>
          }
        </div>
      } @else if (query.length >= 2 && !searching) {
        <p class="empty-msg">No users found</p>
      } @else if (query.length < 2) {
        <p class="hint-msg">Type at least 2 characters to search</p>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Done</button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content { min-width: 400px; max-height: 60vh; padding: 8px 24px; }
    .search-row {
      display: flex; align-items: center; gap: 8px; margin-bottom: 12px;
    }
    .search-input {
      flex: 1; border: 1px solid #ccc; border-radius: 4px;
      padding: 8px 12px; font-size: 14px; outline: none;
    }
    .search-input:focus { border-color: #1976d2; }
    .search-spinner { flex-shrink: 0; }
    .results-list { display: flex; flex-direction: column; gap: 4px; }
    .user-row {
      display: flex; align-items: center; gap: 8px;
      padding: 8px 10px; border-radius: 6px; background: #f5f5f5;
    }
    .user-info { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .user-name { font-size: 14px; font-weight: 500; }
    .user-email { font-size: 12px; color: #757575; }
    .invite-btn { height: 32px; font-size: 12px; }
    .invited-badge {
      display: flex; align-items: center; gap: 4px;
      font-size: 12px; color: #4caf50; white-space: nowrap;
    }
    .invited-badge mat-icon { font-size: 16px; width: 16px; height: 16px; }
    .already-badge { font-size: 12px; color: #9e9e9e; white-space: nowrap; }
    .empty-msg, .hint-msg {
      text-align: center; font-size: 13px; color: rgba(0,0,0,.4);
      margin: 16px 0;
    }
    mat-dialog-actions { padding: 8px 24px 16px; }
  `],
})
export class InviteUsersDialogComponent implements OnDestroy {
  query = '';
  results: UserInfo[] = [];
  searching = false;
  invited = new Set<string>();
  inviting = new Set<string>();

  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly query$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(@Inject(MAT_DIALOG_DATA) public data: InviteUsersDialogData) {
    this.query$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => {
        if (q.length < 2) {
          this.results = [];
          this.searching = false;
          return of([]);
        }
        this.searching = true;
        return this.api.searchUsers(q).pipe(
          catchError(() => of([] as UserInfo[]))
        );
      }),
      takeUntil(this.destroy$),
    ).subscribe(users => {
      this.results = users;
      this.searching = false;
    });
  }

  onQueryChange(value: string): void {
    if (value.length < 2) {
      this.results = [];
      this.searching = false;
    } else {
      this.searching = true;
    }
    this.query$.next(value);
  }

  invite(user: UserInfo): void {
    this.inviting.add(user.id);
    this.api.inviteParticipant(this.data.roomId, { userId: user.id, displayName: user.displayName })
      .subscribe({
        next: (_invite: RoomInviteDto) => {
          this.inviting.delete(user.id);
          this.invited.add(user.id);
        },
        error: () => {
          this.inviting.delete(user.id);
          this.snackBar.open(`Failed to invite ${user.displayName}`, 'Dismiss', { duration: 3000 });
        },
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
