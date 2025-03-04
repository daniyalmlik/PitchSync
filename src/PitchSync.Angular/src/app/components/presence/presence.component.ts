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
        <span class="online-label">{{ onlineUsers.length }} online</span>
      </div>

      @if (onlineUsers.length > 0) {
        <div class="avatars-row">
          @for (user of visibleUsers; track user.userId) {
            <div class="avatar-chip" [matTooltip]="user.displayName">
              {{ initials(user.displayName) }}
            </div>
          }
          @if (onlineUsers.length > maxVisible) {
            <div class="avatar-chip overflow-chip"
                 [matTooltip]="extraTooltip">
              +{{ onlineUsers.length - maxVisible }}
            </div>
          }
        </div>
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
    .online-label { font-size: 13px; color: rgba(0,0,0,.6); }
    .avatars-row { display: flex; flex-wrap: wrap; gap: 4px; }
    .avatar-chip {
      width: 32px; height: 32px; border-radius: 50%;
      background: #1976d2; color: white;
      display: flex; align-items: center; justify-content: center;
      font-size: 11px; font-weight: 600; cursor: default; user-select: none;
    }
    .overflow-chip { background: #757575; }
  `],
})
export class PresenceComponent {
  @Input() onlineUsers: OnlineUserDto[] = [];
  @Input() connectionState: ConnectionState = 'disconnected';

  readonly maxVisible = 5;

  get visibleUsers(): OnlineUserDto[] {
    return this.onlineUsers.slice(0, this.maxVisible);
  }

  get extraTooltip(): string {
    return this.onlineUsers
      .slice(this.maxVisible)
      .map(u => u.displayName)
      .join(', ');
  }

  initials(displayName: string): string {
    return displayName
      .split(' ')
      .slice(0, 2)
      .map(w => w[0]?.toUpperCase() ?? '')
      .join('');
  }
}
