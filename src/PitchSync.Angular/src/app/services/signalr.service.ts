import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { MatchEventResponse } from '../models/event.model';
import { OnlineUserDto, MatchStatus } from '../models/match.model';
import { PlayerRatingResponse } from '../models/rating.model';

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;

  private readonly _presenceUpdate$ = new Subject<{ roomId: string; onlineUsers: OnlineUserDto[] }>();
  private readonly _eventPosted$ = new Subject<MatchEventResponse>();
  private readonly _scoreUpdated$ = new Subject<{ roomId: string; homeScore: number; awayScore: number }>();
  private readonly _statusChanged$ = new Subject<{ roomId: string; status: MatchStatus }>();
  private readonly _ratingsUpdated$ = new Subject<{ roomId: string; allRatings: PlayerRatingResponse[] }>();
  private readonly _eventDeleted$ = new Subject<string>();

  readonly presenceUpdate$: Observable<{ roomId: string; onlineUsers: OnlineUserDto[] }> = this._presenceUpdate$.asObservable();
  readonly eventPosted$: Observable<MatchEventResponse> = this._eventPosted$.asObservable();
  readonly scoreUpdated$: Observable<{ roomId: string; homeScore: number; awayScore: number }> = this._scoreUpdated$.asObservable();
  readonly statusChanged$: Observable<{ roomId: string; status: MatchStatus }> = this._statusChanged$.asObservable();
  readonly ratingsUpdated$: Observable<{ roomId: string; allRatings: PlayerRatingResponse[] }> = this._ratingsUpdated$.asObservable();
  readonly eventDeleted$: Observable<string> = this._eventDeleted$.asObservable();

  async connect(roomId: string): Promise<void> {
    await this.disconnect();

    const token = this.auth.getToken();
    const url = `${environment.gatewayUrl}/hubs/match?roomId=${roomId}&access_token=${token}`;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();
    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  async postEvent(request: object): Promise<void> {
    await this.connection?.invoke('PostEvent', request);
  }

  async updateScore(homeScore: number, awayScore: number): Promise<void> {
    await this.connection?.invoke('UpdateScore', homeScore, awayScore);
  }

  async updateStatus(status: MatchStatus): Promise<void> {
    await this.connection?.invoke('UpdateStatus', status);
  }

  async ratePlayer(playerName: string, team: string, rating: number): Promise<void> {
    await this.connection?.invoke('RatePlayer', playerName, team, rating);
  }

  async deleteEvent(eventId: string): Promise<void> {
    await this.connection?.invoke('DeleteEvent', eventId);
  }

  private registerHandlers(): void {
    if (!this.connection) return;

    this.connection.on('PresenceUpdate', (roomId: string, onlineUsers: OnlineUserDto[]) => {
      this._presenceUpdate$.next({ roomId, onlineUsers });
    });

    this.connection.on('EventPosted', (ev: MatchEventResponse) => {
      this._eventPosted$.next(ev);
    });

    this.connection.on('ScoreUpdated', (roomId: string, homeScore: number, awayScore: number) => {
      this._scoreUpdated$.next({ roomId, homeScore, awayScore });
    });

    this.connection.on('StatusChanged', (roomId: string, status: MatchStatus) => {
      this._statusChanged$.next({ roomId, status });
    });

    this.connection.on('RatingsUpdated', (roomId: string, allRatings: PlayerRatingResponse[]) => {
      this._ratingsUpdated$.next({ roomId, allRatings });
    });

    this.connection.on('EventDeleted', (eventId: string) => {
      this._eventDeleted$.next(eventId);
    });
  }
}
