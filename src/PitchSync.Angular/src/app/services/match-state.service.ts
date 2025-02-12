import { Injectable, inject, OnDestroy } from '@angular/core';
import { BehaviorSubject, map, Subscription } from 'rxjs';
import { ApiService } from './api.service';
import { SignalrService } from './signalr.service';
import { MatchRoomResponse, OnlineUserDto, ParticipantDto, PlayerLineupDto } from '../models/match.model';
import { MatchEventResponse } from '../models/event.model';
import { PlayerRatingResponse } from '../models/rating.model';

@Injectable({ providedIn: 'root' })
export class MatchStateService implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly signalr = inject(SignalrService);

  private readonly _room$ = new BehaviorSubject<MatchRoomResponse | null>(null);
  private readonly _onlineUsers$ = new BehaviorSubject<OnlineUserDto[]>([]);
  private readonly subs = new Subscription();

  readonly currentRoom$ = this._room$.asObservable();
  readonly events$ = this._room$.pipe(map(r => r?.events ?? []));
  readonly participants$ = this._room$.pipe(map(r => r?.participants ?? []));
  readonly homeLineup$ = this._room$.pipe(map(r => r?.homeLineup ?? []));
  readonly awayLineup$ = this._room$.pipe(map(r => r?.awayLineup ?? []));
  readonly onlineUsers$ = this._onlineUsers$.asObservable();

  async loadRoom(id: string): Promise<void> {
    this._room$.next(null);
    this._onlineUsers$.next([]);
    this.subs.unsubscribe();

    const room = await this.api.getRoom(id).toPromise();
    this._room$.next(room ?? null);

    await this.signalr.connect(id);
    this.subscribeToSignalR();
  }

  async unload(): Promise<void> {
    this.subs.unsubscribe();
    await this.signalr.disconnect();
    this._room$.next(null);
    this._onlineUsers$.next([]);
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  private subscribeToSignalR(): void {
    this.subs.add(
      this.signalr.presenceUpdate$.subscribe(({ onlineUsers }) => {
        this._onlineUsers$.next(onlineUsers);
      })
    );

    this.subs.add(
      this.signalr.eventPosted$.subscribe(ev => {
        this.patchRoom(r => ({ ...r, events: [...r.events, ev] }));
      })
    );

    this.subs.add(
      this.signalr.scoreUpdated$.subscribe(({ homeScore, awayScore }) => {
        this.patchRoom(r => ({ ...r, homeScore, awayScore }));
      })
    );

    this.subs.add(
      this.signalr.statusChanged$.subscribe(({ status }) => {
        this.patchRoom(r => ({ ...r, status }));
      })
    );

    this.subs.add(
      this.signalr.ratingsUpdated$.subscribe(() => {
        // Ratings are fetched on demand via ApiService — no state patch needed here
      })
    );

    this.subs.add(
      this.signalr.eventDeleted$.subscribe(eventId => {
        this.patchRoom(r => ({ ...r, events: r.events.filter((e: MatchEventResponse) => e.id !== eventId) }));
      })
    );
  }

  private patchRoom(fn: (r: MatchRoomResponse) => MatchRoomResponse): void {
    const current = this._room$.value;
    if (current) this._room$.next(fn(current));
  }
}
