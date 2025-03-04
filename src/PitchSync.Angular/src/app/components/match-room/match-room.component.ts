import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';
import { MatchStateService } from '../../services/match-state.service';
import { SignalrService } from '../../services/signalr.service';
import { ScoreboardComponent } from '../scoreboard/scoreboard.component';
import { EventFeedComponent } from '../event-feed/event-feed.component';
import { EventFormComponent } from '../event-form/event-form.component';
import { LineupPanelComponent } from '../lineup-panel/lineup-panel.component';
import { RatingsPanelComponent } from '../ratings-panel/ratings-panel.component';
import { PresenceComponent } from '../presence/presence.component';

@Component({
  selector: 'app-match-room',
  standalone: true,
  imports: [
    CommonModule, AsyncPipe,
    MatProgressSpinnerModule, MatIconModule, MatButtonModule,
    ScoreboardComponent, EventFeedComponent, EventFormComponent,
    LineupPanelComponent, RatingsPanelComponent, PresenceComponent,
  ],
  template: `
    @if (loading) {
      <div class="loading-center">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else if (vm$ | async; as vm) {
      <div class="room-layout">

        <!-- Connection state indicator -->
        <div class="connection-indicator" [class]="'conn-' + (connectionState$ | async)">
          <span class="conn-dot"></span>
          <span class="conn-label">{{ connLabel(connectionState$ | async) }}</span>
        </div>

        <!-- Main column -->
        <div class="main-col">
          <app-scoreboard
            [room]="vm.room"
            [currentUserRole]="vm.role">
          </app-scoreboard>

          <div class="section-card">
            <h3 class="section-title">Events</h3>
            <app-event-feed
              [events]="vm.events"
              [currentUserRole]="vm.role"
              [roomId]="vm.room?.id ?? ''">
            </app-event-feed>
          </div>

          @if (vm.role === 'Host' || vm.role === 'Commentator') {
            <div class="section-card">
              <h3 class="section-title">Post Event</h3>
              <app-event-form
                [homeLineup]="vm.homeLineup"
                [awayLineup]="vm.awayLineup"
                [homeTeam]="vm.room?.homeTeam ?? ''"
                [awayTeam]="vm.room?.awayTeam ?? ''">
              </app-event-form>
            </div>
          }
        </div>

        <!-- Sidebar -->
        <div class="sidebar-col">
          <div class="section-card">
            <app-presence
              [onlineUsers]="vm.onlineUsers"
              [connectionState]="(connectionState$ | async) ?? 'disconnected'">
            </app-presence>
          </div>

          <div class="section-card">
            <h3 class="section-title">Lineups</h3>
            <app-lineup-panel
              [homeLineup]="vm.homeLineup"
              [awayLineup]="vm.awayLineup"
              [homeTeam]="vm.room?.homeTeam ?? ''"
              [awayTeam]="vm.room?.awayTeam ?? ''"
              [currentUserRole]="vm.role"
              [roomId]="vm.room?.id ?? ''">
            </app-lineup-panel>
          </div>

          <div class="section-card">
            <h3 class="section-title">Player Ratings</h3>
            <app-ratings-panel
              [ratings]="vm.ratings"
              [currentUserRole]="vm.role"
              [homeTeam]="vm.room?.homeTeam ?? ''"
              [awayTeam]="vm.room?.awayTeam ?? ''">
            </app-ratings-panel>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .loading-center {
      display: flex; align-items: center; justify-content: center;
      min-height: 100vh;
    }
    .room-layout {
      max-width: 1280px; margin: 0 auto; padding: 16px;
      display: grid;
      grid-template-columns: 1fr 360px;
      grid-template-rows: auto 1fr;
      gap: 16px;
      position: relative;
    }
    .connection-indicator {
      grid-column: 1 / -1;
      display: flex; align-items: center; gap: 6px;
      font-size: 12px; justify-content: flex-end;
    }
    .conn-dot {
      width: 8px; height: 8px; border-radius: 50%;
    }
    .conn-connected .conn-dot { background: #4caf50; }
    .conn-reconnecting .conn-dot { background: #ff9800; animation: pulse .8s infinite; }
    .conn-disconnected .conn-dot { background: #f44336; }
    .conn-connected .conn-label { color: #4caf50; }
    .conn-reconnecting .conn-label { color: #ff9800; }
    .conn-disconnected .conn-label { color: #f44336; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: .3; } }
    .main-col {
      grid-column: 1; display: flex; flex-direction: column; gap: 16px;
    }
    .sidebar-col {
      grid-column: 2; display: flex; flex-direction: column; gap: 16px;
    }
    .section-card {
      background: white; border-radius: 8px; padding: 16px;
      box-shadow: 0 1px 4px rgba(0,0,0,.1);
    }
    .section-title {
      margin: 0 0 12px; font-size: 14px; font-weight: 700;
      text-transform: uppercase; letter-spacing: .6px; color: #555;
    }
    @media (max-width: 768px) {
      .room-layout {
        grid-template-columns: 1fr;
      }
      .sidebar-col { grid-column: 1; }
    }
  `],
})
export class MatchRoomComponent implements OnInit, OnDestroy {
  private readonly matchState = inject(MatchStateService);
  private readonly signalr = inject(SignalrService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly connectionState$ = this.signalr.connectionState$;

  loading = true;

  readonly vm$ = combineLatest([
    this.matchState.currentRoom$,
    this.matchState.events$,
    this.matchState.homeLineup$,
    this.matchState.awayLineup$,
    this.matchState.onlineUsers$,
    this.matchState.ratings$,
    this.matchState.currentUserRole$,
  ]).pipe(
    map(([room, events, homeLineup, awayLineup, onlineUsers, ratings, role]) => ({
      room, events, homeLineup, awayLineup, onlineUsers, ratings, role,
    }))
  );

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.params['id'] as string;
    try {
      await this.matchState.loadRoom(id);
    } catch {
      await this.router.navigate(['/matches']);
    } finally {
      this.loading = false;
    }
  }

  async ngOnDestroy(): Promise<void> {
    await this.matchState.unload();
  }

  connLabel(state: string | null): string {
    switch (state) {
      case 'connected': return 'Live';
      case 'reconnecting': return 'Reconnecting...';
      default: return 'Offline';
    }
  }
}
