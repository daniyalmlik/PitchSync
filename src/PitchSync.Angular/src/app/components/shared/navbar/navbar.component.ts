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
    <mat-toolbar color="primary">
      <a routerLink="/matches" style="text-decoration:none;color:inherit;font-weight:700;font-size:1.2rem;display:flex;align-items:center">
        <img src="favicon.ico" alt="PitchSync" style="width:24px;height:24px;margin-right:8px"> PitchSync
      </a>
      <span class="spacer"></span>

      @if (auth.currentUser$ | async; as user) {
        <span style="margin-right:8px">{{ user.displayName }}</span>
        @if (user.favoriteTeam) {
          <mat-chip-set style="margin-right:8px">
            <mat-chip highlighted>{{ user.favoriteTeam }}</mat-chip>
          </mat-chip-set>
        }
        <button mat-icon-button (click)="auth.logout()" title="Sign out">
          <mat-icon>logout</mat-icon>
        </button>
      }
    </mat-toolbar>
  `
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
}
