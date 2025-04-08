import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatchEventResponse, MatchEventType } from '../../models/event.model';
import { RoomRole } from '../../models/match.model';
import { SignalrService } from '../../services/signalr.service';
import { EventIconPipe } from '../../pipes/event-icon.pipe';
import { RelativeTimePipe } from '../../pipes/relative-time.pipe';

const GOAL_EVENTS: MatchEventType[] = ['Goal', 'Penalty'];
const OWN_GOAL_EVENTS: MatchEventType[] = ['OwnGoal'];
const CARD_EVENTS: MatchEventType[] = ['YellowCard', 'RedCard'];

@Component({
  selector: 'app-event-feed',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, EventIconPipe, RelativeTimePipe],
  template: `
    <div class="event-feed">
      @if (events.length === 0) {
        <div class="empty-state">
          <mat-icon>sports_soccer</mat-icon>
          <p>No events yet</p>
        </div>
      } @else {
        @for (event of events; track event.id) {
          <div class="event-row" [class]="rowClass(event.eventType)">
            <div class="minute-badge" [class]="'badge-' + badgeColor(event.eventType)">
              {{ event.minute }}'
            </div>

            <div class="event-body">
              <div class="event-main">
                <span class="event-icon">{{ event.eventType | eventIcon }}</span>
                @if (event.playerName) {
                  <span class="player-name">{{ event.playerName }}</span>
                }
                @if (event.secondaryPlayerName) {
                  <span class="secondary"> ↔ {{ event.secondaryPlayerName }}</span>
                }
                @if (event.team) {
                  <span class="team-tag">{{ event.team }}</span>
                }
              </div>
              @if (event.description) {
                <div class="event-desc">{{ event.description }}</div>
              }
              <div class="event-meta">
                {{ event.postedByDisplayName }} · {{ event.createdAt | relativeTime }}
              </div>
            </div>

            @if (isHost) {
              <button mat-icon-button class="delete-btn"
                      (click)="onDelete(event.id)"
                      aria-label="Delete event">
                <mat-icon>delete_outline</mat-icon>
              </button>
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .event-feed {
      display: flex; flex-direction: column; gap: 2px;
      max-height: 60vh; overflow-y: auto; padding: 4px 0;
    }
    .empty-state {
      display: flex; flex-direction: column; align-items: center;
      padding: 32px 16px; color: rgba(0,0,0,.4);
    }
    .empty-state mat-icon { font-size: 40px; width: 40px; height: 40px; margin-bottom: 8px; }
    .empty-state p { margin: 0; }
    .event-row {
      display: flex; align-items: flex-start; gap: 12px;
      padding: 10px 12px; border-radius: 6px;
      background: #fafafa; border-left: 3px solid transparent;
      transition: background .15s;
    }
    .event-row:hover { background: #f0f0f0; }
    .event-row:hover .delete-btn { opacity: 1; }
    .row-goal { border-left-color: #4caf50; background: #f1f8e9; }
    .row-owngoal { border-left-color: #f44336; background: #fce4ec; }
    .row-yellowcard { border-left-color: #ffc107; }
    .row-redcard { border-left-color: #f44336; }
    .minute-badge {
      min-width: 38px; height: 38px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      font-size: 12px; font-weight: 700; color: white; flex-shrink: 0;
    }
    .badge-green { background: #4caf50; }
    .badge-red { background: #f44336; }
    .badge-yellow { background: #ffc107; color: #333; }
    .badge-blue { background: #2196f3; }
    .badge-gray { background: #9e9e9e; }
    .event-body { flex: 1; min-width: 0; }
    .event-main { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .event-icon { font-size: 18px; line-height: 1; flex-shrink: 0; }
    .player-name { font-weight: 600; font-size: 14px; }
    .secondary { font-size: 13px; color: rgba(0,0,0,.6); }
    .team-tag {
      font-size: 11px; padding: 1px 6px; border-radius: 10px;
      background: #e3f2fd; color: #1565c0;
    }
    .event-desc { font-size: 13px; color: rgba(0,0,0,.7); margin-top: 2px; }
    .event-meta { font-size: 11px; color: rgba(0,0,0,.45); margin-top: 2px; }
    .delete-btn { opacity: 0; transition: opacity .15s; color: rgba(0,0,0,.4); }
  `],
})
export class EventFeedComponent {
  @Input() events: MatchEventResponse[] = [];
  @Input() currentUserRole: RoomRole | null = null;
  @Input() roomId = '';

  private readonly signalr = inject(SignalrService);
  private readonly snackBar = inject(MatSnackBar);

  get isHost(): boolean { return this.currentUserRole === 'Host'; }

  rowClass(type: MatchEventType): string {
    if (GOAL_EVENTS.includes(type)) return 'row-goal';
    if (OWN_GOAL_EVENTS.includes(type)) return 'row-owngoal';
    if (type === 'YellowCard') return 'row-yellowcard';
    if (type === 'RedCard') return 'row-redcard';
    return '';
  }

  badgeColor(type: MatchEventType): string {
    if (GOAL_EVENTS.includes(type)) return 'green';
    if (OWN_GOAL_EVENTS.includes(type)) return 'red';
    if (type === 'YellowCard') return 'yellow';
    if (type === 'RedCard') return 'red';
    if (CARD_EVENTS.includes(type)) return 'red';
    return 'blue';
  }

  async onDelete(eventId: string): Promise<void> {
    try {
      await this.signalr.deleteEvent(eventId);
    } catch {
      this.snackBar.open('Failed to delete event', 'Dismiss', { duration: 3000 });
    }
  }
}
