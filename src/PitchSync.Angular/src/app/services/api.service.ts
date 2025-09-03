import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, RegisterRequest, TokenResponse, UserInfo, UpdateProfileRequest } from '../models/user.model';
import {
  CreateMatchRequest, MatchRoomResponse, MatchRoomSummary,
  JoinMatchRequest, UpdateScoreRequest, UpdateStatusRequest,
  ParticipantDto, PlayerLineupDto, SetLineupRequest, PromoteParticipantRequest,
  InviteParticipantRequest, MatchStatus, RoomInviteDto
} from '../models/match.model';
import { PostEventRequest, MatchEventResponse } from '../models/event.model';
import { RatePlayerRequest, PlayerRatingResponse } from '../models/rating.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.gatewayUrl;

  // --- Auth ---
  login(body: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/api/auth/login`, body);
  }

  register(body: RegisterRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/api/auth/register`, body);
  }

  refreshToken(): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/api/auth/refresh`, {});
  }

  // --- Users ---
  getProfile(): Observable<UserInfo> {
    return this.http.get<UserInfo>(`${this.base}/api/users/me`);
  }

  updateProfile(body: UpdateProfileRequest): Observable<UserInfo> {
    return this.http.put<UserInfo>(`${this.base}/api/users/me`, body);
  }

  searchUsers(q: string): Observable<UserInfo[]> {
    const params = new HttpParams().set('q', q);
    return this.http.get<UserInfo[]>(`${this.base}/api/users/search`, { params });
  }

  // --- Rooms ---
  getPublicRooms(page = 1, pageSize = 20, search?: string, status?: MatchStatus): Observable<MatchRoomSummary[]> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    return this.http.get<MatchRoomSummary[]>(`${this.base}/api/matches`, { params });
  }

  getMyRooms(page = 1, pageSize = 20): Observable<MatchRoomSummary[]> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<MatchRoomSummary[]>(`${this.base}/api/matches/mine`, { params });
  }

  getRoom(id: string): Observable<MatchRoomResponse> {
    return this.http.get<MatchRoomResponse>(`${this.base}/api/matches/${id}`);
  }

  createRoom(body: CreateMatchRequest): Observable<MatchRoomResponse> {
    return this.http.post<MatchRoomResponse>(`${this.base}/api/matches`, body);
  }

  deleteRoom(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/matches/${id}`);
  }

  joinRoom(id: string, inviteCode?: string): Observable<ParticipantDto> {
    const body: JoinMatchRequest = inviteCode ? { inviteCode } : {};
    return this.http.post<ParticipantDto>(`${this.base}/api/matches/${id}/join`, body);
  }

  leaveRoom(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/matches/${id}/leave`, {});
  }

  updateStatus(id: string, status: MatchStatus): Observable<MatchRoomResponse> {
    const body: UpdateStatusRequest = { status };
    return this.http.patch<MatchRoomResponse>(`${this.base}/api/matches/${id}/status`, body);
  }

  updateScore(id: string, homeScore: number, awayScore: number): Observable<MatchRoomResponse> {
    const body: UpdateScoreRequest = { homeScore, awayScore };
    return this.http.patch<MatchRoomResponse>(`${this.base}/api/matches/${id}/score`, body);
  }

  promoteParticipant(roomId: string, userId: string, body: PromoteParticipantRequest): Observable<ParticipantDto> {
    return this.http.patch<ParticipantDto>(`${this.base}/api/matches/${roomId}/participants/${userId}/role`, body);
  }

  inviteParticipant(roomId: string, body: InviteParticipantRequest): Observable<RoomInviteDto> {
    return this.http.post<RoomInviteDto>(`${this.base}/api/matches/${roomId}/invite`, body);
  }

  getPendingInvites(): Observable<RoomInviteDto[]> {
    return this.http.get<RoomInviteDto[]>(`${this.base}/api/matches/invites`);
  }

  acceptInvite(inviteId: string): Observable<{ matchRoomId: string; participant: ParticipantDto }> {
    return this.http.post<{ matchRoomId: string; participant: ParticipantDto }>(
      `${this.base}/api/matches/invites/${inviteId}/accept`, {}
    );
  }

  declineInvite(inviteId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/matches/invites/${inviteId}/decline`, {});
  }

  // --- Events ---
  postEvent(matchId: string, body: PostEventRequest): Observable<MatchEventResponse> {
    return this.http.post<MatchEventResponse>(`${this.base}/api/matches/${matchId}/events`, body);
  }

  getEvents(matchId: string, page?: number, pageSize?: number): Observable<MatchEventResponse[]> {
    let params = new HttpParams();
    if (page) params = params.set('page', page);
    if (pageSize) params = params.set('pageSize', pageSize);
    return this.http.get<MatchEventResponse[]>(`${this.base}/api/matches/${matchId}/events`, { params });
  }

  deleteEvent(matchId: string, eventId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/matches/${matchId}/events/${eventId}`);
  }

  // --- Lineups ---
  setLineup(matchId: string, team: string, players: PlayerLineupDto[]): Observable<PlayerLineupDto[]> {
    const body: SetLineupRequest = { players };
    return this.http.put<PlayerLineupDto[]>(`${this.base}/api/matches/${matchId}/lineups/${team}`, body);
  }

  getLineups(matchId: string): Observable<{ homeLineup: PlayerLineupDto[]; awayLineup: PlayerLineupDto[] }> {
    return this.http.get<{ homeLineup: PlayerLineupDto[]; awayLineup: PlayerLineupDto[] }>(
      `${this.base}/api/matches/${matchId}/lineups`
    );
  }

  // --- Ratings ---
  ratePlayer(matchId: string, team: string, playerName: string, rating: number): Observable<PlayerRatingResponse> {
    const body: RatePlayerRequest = { rating };
    return this.http.put<PlayerRatingResponse>(
      `${this.base}/api/matches/${matchId}/ratings/${team}/${encodeURIComponent(playerName)}`,
      body
    );
  }

  getRatings(matchId: string): Observable<PlayerRatingResponse[]> {
    return this.http.get<PlayerRatingResponse[]>(`${this.base}/api/matches/${matchId}/ratings`);
  }

  getMyRatings(matchId: string): Observable<PlayerRatingResponse[]> {
    return this.http.get<PlayerRatingResponse[]>(`${this.base}/api/matches/${matchId}/ratings/mine`);
  }
}
