import { MatchEventResponse } from './event.model';

export type MatchStatus = 'Upcoming' | 'Live' | 'HalfTime' | 'SecondHalf' | 'FullTime' | 'Abandoned';
export type RoomRole = 'Host' | 'Commentator' | 'Spectator';

export interface CreateMatchRequest {
  title: string;
  homeTeam: string;
  awayTeam: string;
  competition?: string;
  kickoffTime: string;
  isPublic: boolean;
}

export interface MatchRoomResponse {
  id: string;
  title: string;
  homeTeam: string;
  awayTeam: string;
  competition?: string;
  kickoffTime: string;
  status: MatchStatus;
  homeScore: number;
  awayScore: number;
  isPublic: boolean;
  inviteCode?: string;
  createdByUserId: string;
  createdAt: string;
  participants: ParticipantDto[];
  events: MatchEventResponse[];
  homeLineup: PlayerLineupDto[];
  awayLineup: PlayerLineupDto[];
}

export interface MatchRoomSummary {
  id: string;
  title: string;
  homeTeam: string;
  awayTeam: string;
  status: MatchStatus;
  homeScore: number;
  awayScore: number;
  kickoffTime: string;
  participantCount: number;
}

export interface JoinMatchRequest {
  inviteCode?: string;
}

export interface UpdateScoreRequest {
  homeScore: number;
  awayScore: number;
}

export interface UpdateStatusRequest {
  status: MatchStatus;
}

export interface ParticipantDto {
  userId: string;
  displayName: string;
  role: RoomRole;
  joinedAt: string;
}

export interface OnlineUserDto {
  userId: string;
  displayName: string;
  favoriteTeam?: string;
}

export interface PlayerLineupDto {
  playerName: string;
  shirtNumber?: number;
  position?: string;
  isStarting: boolean;
}

export interface SetLineupRequest {
  players: PlayerLineupDto[];
}

export interface PromoteParticipantRequest {
  role: RoomRole;
}
