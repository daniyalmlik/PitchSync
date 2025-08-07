import { Component, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    AsyncPipe,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
  ],
  template: `
    <mat-toolbar color="primary" style="overflow:hidden">
      <a routerLink="/matches" style="text-decoration:none;color:inherit;font-weight:700;font-size:1.2rem;display:flex;align-items:center;flex-shrink:0">
        <mat-icon style="margin-right:8px;font-size:26px;width:26px;height:26px">sports_soccer</mat-icon> PitchSync
      </a>
      <span class="spacer"></span>

      @if (auth.currentUser$ | async; as user) {
        <div style="display:flex;align-items:center;gap:8px;min-width:0;overflow:hidden">
          <span style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:220px">{{ user.displayName }}</span>
          @if (user.favoriteTeam) {
            <mat-chip-set style="flex-shrink:0">
              <mat-chip highlighted>{{ user.favoriteTeam }}</mat-chip>
            </mat-chip-set>
          }
          <button mat-icon-button (click)="auth.logout()" title="Sign out" style="flex-shrink:0">
            <mat-icon>logout</mat-icon>
          </button>
        </div>
      }
    </mat-toolbar>
  `
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
}
