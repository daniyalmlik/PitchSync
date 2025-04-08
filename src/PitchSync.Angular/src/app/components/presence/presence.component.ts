import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { OnlineUserDto } from '../../models/match.model';
import { ConnectionState } from '../../services/signalr.service';

@Component({
  selector: 'app-presence',
  standalone: true,
  imports: [CommonModule, MatTooltipModule],
  template: `
    <div class="presence-container">
      <div class="presence-header">
        <span class="status-dot" [class]="'dot-' + connectionState"></span>
        <span class="online-label">{{ onlineUsers.length }} watching</span>
        @if (onlineUsers.length > 0) {
          <button class="toggle-btn" (click)="expanded = !expanded">
            {{ expanded ? 'Hide' : 'Show all' }}
          </button>
        }
      </div>

      @if (onlineUsers.length > 0) {
        <div class="avatars-row">
          @for (user of visibleUsers; track user.userId) {
            <div class="avatar-wrap"
                 [matTooltip]="tooltipFor(user)"
                 matTooltipPosition="above">
              <div class="avatar-chip">{{ initials(user.displayName) }}</div>
              <span class="online-dot"></span>
            </div>
          }
          @if (!expanded && onlineUsers.length > maxVisible) {
            <div class="avatar-chip overflow-chip"
                 [matTooltip]="extraTooltip"
                 matTooltipPosition="above">
              +{{ onlineUsers.length - maxVisible }}
            </div>
          }
        </div>

        @if (expanded) {
          <div class="expanded-list">
            @for (user of onlineUsers; track user.userId) {
              <div class="user-row">
                <div class="avatar-chip avatar-sm">{{ initials(user.displayName) }}</div>
                <div class="user-info">
                  <span class="user-name">{{ user.displayName }}</span>
                  @if (user.favoriteTeam) {
                    <span class="user-team">{{ user.favoriteTeam }}</span>
                  }
                </div>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .presence-container { padding: 8px 0; }
    .presence-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
    .status-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
    .dot-connected { background: #4caf50; }
    .dot-reconnecting { background: #ff9800; animation: pulse 1s infinite; }
    .dot-disconnected { background: #f44336; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }
    .online-label { font-size: 13px; color: rgba(0,0,0,.6); flex: 1; }
    .toggle-btn {
      background: none; border: none; padding: 0;
      font-size: 12px; color: #1976d2; cursor: pointer;
      text-decoration: underline;
    }
    .avatars-row { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 4px; }
    .avatar-wrap { position: relative; display: inline-flex; }
    .avatar-chip {
      width: 34px; height: 34px; border-radius: 50%;
      background: #1976d2; color: white;
      display: flex; align-items: center; justify-content: center;
      font-size: 11px; font-weight: 600; cursor: default; user-select: none;
    }
    .avatar-sm { width: 26px; height: 26px; font-size: 10px; flex-shrink: 0; }
    .overflow-chip { background: #757575; cursor: default; }
    .online-dot {
      position: absolute; bottom: 1px; right: 1px;
      width: 10px; height: 10px; border-radius: 50%;
      background: #4caf50; border: 2px solid white;
    }
    .expanded-list {
      margin-top: 8px; display: flex; flex-direction: column; gap: 4px;
      max-height: 180px; overflow-y: auto;
    }
    .user-row { display: flex; align-items: center; gap: 8px; padding: 3px 0; }
    .user-info { display: flex; flex-direction: column; }
    .user-name { font-size: 13px; font-weight: 500; }
    .user-team { font-size: 11px; color: rgba(0,0,0,.5); }
  `],
})
export class PresenceComponent {
  @Input() onlineUsers: OnlineUserDto[] = [];
  @Input() connectionState: ConnectionState = 'disconnected';

  readonly maxVisible = 5;
  expanded = false;

  get visibleUsers(): OnlineUserDto[] {
    return this.expanded ? this.onlineUsers : this.onlineUsers.slice(0, this.maxVisible);
  }

  get extraTooltip(): string {
    return this.onlineUsers
      .slice(this.maxVisible)
      .map(u => u.displayName)
      .join(', ');
  }

  tooltipFor(user: OnlineUserDto): string {
    return user.favoriteTeam ? `${user.displayName} · ${user.favoriteTeam}` : user.displayName;
  }

  initials(displayName: string): string {
    return displayName
      .split(' ')
      .slice(0, 2)
      .map(w => w[0]?.toUpperCase() ?? '')
      .join('');
  }
}
