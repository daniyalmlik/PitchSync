export type MatchEventType =
  | 'Goal'
  | 'OwnGoal'
  | 'Assist'
  | 'YellowCard'
  | 'RedCard'
  | 'Substitution'
  | 'Penalty'
  | 'PenaltyMiss'
  | 'VAR'
  | 'Injury'
  | 'HalfTime'
  | 'FullTime'
  | 'KickOff'
  | 'FreeKick'
  | 'Corner'
  | 'Save'
  | 'Comment';

export interface PostEventRequest {
  minute: number;
  eventType: MatchEventType;
  team?: string;
  playerName?: string;
  secondaryPlayerName?: string;
  description?: string;
}

export interface MatchEventResponse {
  id: string;
  minute: number;
  eventType: MatchEventType;
  team?: string;
  playerName?: string;
  secondaryPlayerName?: string;
  description?: string;
  postedByUserId: string;
  postedByDisplayName: string;
  createdAt: string;
}
