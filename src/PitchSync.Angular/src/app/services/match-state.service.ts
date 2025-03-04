import { Injectable, inject, OnDestroy } from '@angular/core';
import { BehaviorSubject, combineLatest, map, Subscription } from 'rxjs';
import { ApiService } from './api.service';
import { SignalrService } from './signalr.service';
import { AuthService } from './auth.service';
import { MatchRoomResponse, OnlineUserDto, ParticipantDto, PlayerLineupDto, RoomRole } from '../models/match.model';
import { MatchEventResponse, PostEventRequest } from '../models/event.model';
import { PlayerRatingResponse } from '../models/rating.model';

@Injectable({ providedIn: 'root' })
export class MatchStateService implements OnDestroy {
  private readonly api = inject(ApiService);
  private readonly signalr = inject(SignalrService);
  private readonly auth = inject(AuthService);

  private readonly _room$ = new BehaviorSubject<MatchRoomResponse | null>(null);
  private readonly _onlineUsers$ = new BehaviorSubject<OnlineUserDto[]>([]);
  private readonly _ratings$ = new BehaviorSubject<PlayerRatingResponse[]>([]);
  private subs = new Subscription();

  readonly currentRoom$ = this._room$.asObservable();
  readonly events$ = this._room$.pipe(map(r => (r?.events ?? []).slice().sort((a, b) => b.minute - a.minute)));
  readonly participants$ = this._room$.pipe(map(r => r?.participants ?? []));
  readonly homeLineup$ = this._room$.pipe(map(r => r?.homeLineup ?? []));
  readonly awayLineup$ = this._room$.pipe(map(r => r?.awayLineup ?? []));
  readonly onlineUsers$ = this._onlineUsers$.asObservable();
  readonly ratings$ = this._ratings$.asObservable();

  readonly currentUserRole$ = combineLatest([this._room$, this.auth.currentUser$]).pipe(
    map(([room, user]) => {
      if (!room || !user) return null;
      return room.participants.find(p => p.userId === user.id)?.role ?? null;
    })
  );

  readonly score$ = this._room$.pipe(
    map(r => r ? { homeScore: r.homeScore, awayScore: r.awayScore } : null)
  );

  async loadRoom(id: string): Promise<void> {
    this._room$.next(null);
    this._onlineUsers$.next([]);
    this._ratings$.next([]);
    this.subs.unsubscribe();
    this.subs = new Subscription();

    const room = await this.api.getRoom(id).toPromise();
    this._room$.next(room ?? null);

    const ratings = await this.api.getRatings(id).toPromise();
    this._ratings$.next(ratings ?? []);

    await this.signalr.connect(id);
    this.subscribeToSignalR();
  }

  async unload(): Promise<void> {
    this.subs.unsubscribe();
    this.subs = new Subscription();
    await this.signalr.disconnect();
    this._room$.next(null);
    this._onlineUsers$.next([]);
    this._ratings$.next([]);
  }

  async postEventOptimistic(request: PostEventRequest): Promise<void> {
    const tempId = 'temp-' + Date.now();
    const user = this.auth.getCurrentUser();
    const tempEvent: MatchEventResponse = {
      id: tempId,
      minute: request.minute,
      eventType: request.eventType,
      team: request.team,
      playerName: request.playerName,
      secondaryPlayerName: request.secondaryPlayerName,
      description: request.description,
      postedByUserId: user?.id ?? '',
      postedByDisplayName: user?.displayName ?? '',
      createdAt: new Date().toISOString(),
    };

    this.patchRoom(r => ({ ...r, events: [...r.events, tempEvent] }));

    try {
      const real = await this.api.postEvent(this._room$.value!.id, request).toPromise();
      this.patchRoom(r => ({
        ...r,
        events: r.events.map(e => e.id === tempId ? real! : e),
      }));
    } catch (error) {
      this.patchRoom(r => ({ ...r, events: r.events.filter(e => e.id !== tempId) }));
      throw error;
    }
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
      this.signalr.ratingsUpdated$.subscribe(({ roomId }) => {
        this.api.getRatings(roomId).subscribe(ratings => this._ratings$.next(ratings));
      })
    );

    this.subs.add(
      this.signalr.eventDeleted$.subscribe(eventId => {
        this.patchRoom(r => ({ ...r, events: r.events.filter((e: MatchEventResponse) => e.id !== eventId) }));
      })
    );

    this.subs.add(
      this.signalr.participantJoined$.subscribe(({ participant }) => {
        this.patchRoom(r => ({
          ...r,
          participants: r.participants.some(p => p.userId === participant.userId)
            ? r.participants.map(p => p.userId === participant.userId ? participant : p)
            : [...r.participants, participant],
        }));
      })
    );

    this.subs.add(
      this.signalr.participantLeft$.subscribe(({ userId }) => {
        this.patchRoom(r => ({
          ...r,
          participants: r.participants.filter((p: ParticipantDto) => p.userId !== userId),
        }));
      })
    );
  }

  private patchRoom(fn: (r: MatchRoomResponse) => MatchRoomResponse): void {
    const current = this._room$.value;
    if (current) this._room$.next(fn(current));
  }
}
