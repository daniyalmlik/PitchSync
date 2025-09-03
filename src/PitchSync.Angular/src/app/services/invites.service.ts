import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, Subscription, map } from 'rxjs';
import { ApiService } from './api.service';
import { SignalrService } from './signalr.service';
import { RoomInviteDto } from '../models/match.model';

@Injectable({ providedIn: 'root' })
export class InvitesService {
  private readonly api = inject(ApiService);
  private readonly signalr = inject(SignalrService);

  private readonly _pendingInvites$ = new BehaviorSubject<RoomInviteDto[]>([]);
  readonly pendingInvites$: Observable<RoomInviteDto[]> = this._pendingInvites$.asObservable();
  readonly pendingCount$: Observable<number> = this._pendingInvites$.pipe(map(invites => invites.length));

  private signalrSub: Subscription | null = null;

  load(): void {
    this.api.getPendingInvites().subscribe({
      next: invites => this._pendingInvites$.next(invites),
      error: () => {}
    });

    if (!this.signalrSub) {
      this.signalrSub = this.signalr.inviteReceived$.subscribe(invite => {
        const current = this._pendingInvites$.value;
        if (!current.some(i => i.id === invite.id)) {
          this._pendingInvites$.next([invite, ...current]);
        }
      });
    }
  }

  removeInvite(inviteId: string): void {
    this._pendingInvites$.next(
      this._pendingInvites$.value.filter(i => i.id !== inviteId)
    );
  }

  clear(): void {
    this._pendingInvites$.next([]);
    this.signalrSub?.unsubscribe();
    this.signalrSub = null;
  }
}
