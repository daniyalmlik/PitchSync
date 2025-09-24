import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { timeout } from 'rxjs/operators';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { MatchRoomSummary, MatchStatus, PagedResult } from '../../models/match.model';
import { InviteCodeDialogComponent } from '../invite-code-dialog/invite-code-dialog.component';

@Component({
  selector: 'app-match-browser',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatTabsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatChipsModule,
    MatProgressSpinnerModule, MatDialogModule,
  ],
  template: `
    <div class="browser-page">
      <div class="browser-header">
        <h1 class="page-title">Match Rooms</h1>
      </div>

      <mat-tab-group (selectedIndexChange)="onTabChange($event)">
        <mat-tab label="All Rooms">
          <ng-template matTabContent>
            <div class="filter-row">
              <mat-form-field appearance="outline" class="search-field">
                <mat-icon matPrefix>search</mat-icon>
                <input matInput [(ngModel)]="searchQuery" (ngModelChange)="onSearchChange()"
                       placeholder="Search rooms..." />
              </mat-form-field>

              <mat-form-field appearance="outline" class="status-field">
                <mat-label>Status</mat-label>
                <mat-select [(ngModel)]="statusFilter" (ngModelChange)="loadPublic()">
                  <mat-option value="">All</mat-option>
                  @for (s of statuses; track s.value) {
                    <mat-option [value]="s.value">{{ s.label }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            @if (loadingPublic) {
              <div class="spinner-center"><mat-spinner diameter="40"></mat-spinner></div>
            } @else if (publicRooms.length === 0) {
              <p class="empty-state">No rooms found</p>
            } @else {
              <div class="rooms-grid">
                @for (room of publicRooms; track room.id) {
                  <mat-card class="room-card" (click)="openRoom(room)">
                    <mat-card-content>
                      <div class="room-status-row">
                        <span class="status-chip" [class]="'chip-' + (room.status + '').toLowerCase()">
                          {{ room.status }}
                        </span>
                        <span class="participant-count">
                          <mat-icon class="small-icon">people</mat-icon>
                          {{ room.participantCount }}
                        </span>
                      </div>
                      <div class="teams-display">
                        <span class="team">{{ room.homeTeam }}</span>
                        <div class="score-block">
                          <span class="score">{{ room.homeScore }}</span>
                          <span class="score-sep">–</span>
                          <span class="score">{{ room.awayScore }}</span>
                        </div>
                        <span class="team away">{{ room.awayTeam }}</span>
                      </div>
                      <div class="room-meta">
                        {{ room.kickoffTime | date:'EEE d MMM, HH:mm' }}
                      </div>
                    </mat-card-content>
                  </mat-card>
                }
              </div>
            }
          </ng-template>
        </mat-tab>

        <mat-tab label="My Rooms">
          <ng-template matTabContent>
            @if (loadingMy) {
              <div class="spinner-center"><mat-spinner diameter="40"></mat-spinner></div>
            } @else if (myRooms.length === 0) {
              <p class="empty-state">You haven't joined any rooms yet</p>
            } @else {
              <div class="rooms-grid">
                @for (room of myRooms; track room.id) {
                  <mat-card class="room-card" (click)="openRoom(room)">
                    <mat-card-content>
                      <div class="room-status-row">
                        <span class="status-chip" [class]="'chip-' + (room.status + '').toLowerCase()">
                          {{ room.status }}
                        </span>
                        <span class="participant-count">
                          <mat-icon class="small-icon">people</mat-icon>
                          {{ room.participantCount }}
                        </span>
                      </div>
                      <div class="teams-display">
                        <span class="team">{{ room.homeTeam }}</span>
                        <div class="score-block">
                          <span class="score">{{ room.homeScore }}</span>
                          <span class="score-sep">–</span>
                          <span class="score">{{ room.awayScore }}</span>
                        </div>
                        <span class="team away">{{ room.awayTeam }}</span>
                      </div>
                      <div class="room-meta">
                        {{ room.kickoffTime | date:'EEE d MMM, HH:mm' }}
                      </div>
                    </mat-card-content>
                  </mat-card>
                }
              </div>
            }
          </ng-template>
        </mat-tab>
      </mat-tab-group>

      <button mat-fab color="primary" class="fab-create" routerLink="/matches/new"
              aria-label="Create room">
        <mat-icon>add</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    .browser-page { padding: 16px; max-width: 1200px; margin: 0 auto; position: relative; min-height: 100vh; }
    .browser-header { margin-bottom: 8px; }
    .page-title { margin: 0 0 16px; font-size: 24px; font-weight: 700; }
    .filter-row { display: flex; gap: 12px; flex-wrap: wrap; padding: 12px 0 4px; }
    .search-field { flex: 1; min-width: 200px; }
    .status-field { width: 160px; }
    .spinner-center { display: flex; justify-content: center; padding: 40px; }
    .empty-state { text-align: center; color: rgba(0,0,0,.4); padding: 40px 16px; }
    .rooms-grid {
      display: grid; gap: 12px; padding: 12px 0 80px;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    }
    .room-card { cursor: pointer; transition: box-shadow .2s; }
    .room-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,.18); }
    .room-status-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
    .status-chip {
      font-size: 10px; font-weight: 700; padding: 2px 8px; border-radius: 10px;
      text-transform: uppercase; letter-spacing: .6px;
    }
    .chip-upcoming { background: #e3f2fd; color: #1565c0; }
    .chip-live { background: #e8f5e9; color: #1b5e20; }
    .chip-halftime { background: #fff3e0; color: #e65100; }
    .chip-secondhalf { background: #e8f5e9; color: #1b5e20; }
    .chip-fulltime { background: #f3e5f5; color: #4a148c; }
    .chip-abandoned { background: #ffebee; color: #b71c1c; }
    .participant-count { display: flex; align-items: center; gap: 2px; font-size: 12px; color: rgba(0,0,0,.5); }
    .small-icon { font-size: 14px; width: 14px; height: 14px; }
    .teams-display { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
    .team { font-size: 14px; font-weight: 600; flex: 1; }
    .team.away { text-align: right; }
    .score-block { display: flex; align-items: center; gap: 4px; }
    .score { font-size: 22px; font-weight: 800; }
    .score-sep { font-size: 18px; color: rgba(0,0,0,.4); }
    .room-meta { font-size: 11px; color: rgba(0,0,0,.45); }
    .fab-create { position: fixed; bottom: 24px; right: 24px; z-index: 100; }
  `],
})
export class MatchBrowserComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  publicRooms: MatchRoomSummary[] = [];
  myRooms: MatchRoomSummary[] = [];
  loadingPublic = false;
  loadingMy = false;
  searchQuery = '';
  statusFilter: MatchStatus | '' = '';

  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly statuses: { value: MatchStatus; label: string }[] = [
    { value: 'Upcoming', label: 'Upcoming' },
    { value: 'Live', label: 'Live' },
    { value: 'HalfTime', label: 'Half Time' },
    { value: 'SecondHalf', label: '2nd Half' },
    { value: 'FullTime', label: 'Full Time' },
  ];

  ngOnInit(): void {
    const resolved: PagedResult<MatchRoomSummary> | null = this.route.snapshot.data['rooms'] ?? null;
    if (resolved && resolved.items.length > 0) {
      this.publicRooms = resolved.items;
    } else {
      this.loadPublic();
    }
    this.loadMy();
  }

  onTabChange(index: number): void {
    if (index === 1 && this.myRooms.length === 0) {
      this.loadMy();
    }
  }

  onSearchChange(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.loadPublic(), 400);
  }

  loadPublic(): void {
    this.loadingPublic = true;
    this.api.getPublicRooms(1, 50, this.searchQuery || undefined, this.statusFilter || undefined)
      .pipe(timeout(15_000), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => { this.publicRooms = result.items; this.loadingPublic = false; },
        error: () => {
          this.loadingPublic = false;
          this.snackBar.open('Failed to load rooms. Please try again.', 'Retry', { duration: 5000 })
            .onAction().subscribe(() => this.loadPublic());
        },
      });
  }

  loadMy(): void {
    this.loadingMy = true;
    this.api.getMyRooms(1, 50)
      .pipe(timeout(10_000), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => { this.myRooms = result.items; this.loadingMy = false; },
        error: () => {
          this.loadingMy = false;
          this.snackBar.open('Failed to load your rooms.', 'Dismiss', { duration: 4000 });
        },
      });
  }

  async openRoom(room: MatchRoomSummary): Promise<void> {
    try {
      await this.api.joinRoom(room.id).toPromise();
      await this.router.navigate(['/matches', room.id]);
    } catch (err: unknown) {
      const status = (err as { status?: number })?.status;
      if (status === 403) {
        const code = await this.promptInviteCode();
        if (code == null) return;
        try {
          await this.api.joinRoom(room.id, code).toPromise();
          await this.router.navigate(['/matches', room.id]);
        } catch {
          this.snackBar.open('Invalid invite code', 'Dismiss', { duration: 3000 });
        }
      } else {
        await this.router.navigate(['/matches', room.id]);
      }
    }
  }

  private promptInviteCode(): Promise<string | null> {
    const ref = this.dialog.open(InviteCodeDialogComponent, { width: '320px' });
    return ref.afterClosed().toPromise().then(v => v ?? null);
  }
}
