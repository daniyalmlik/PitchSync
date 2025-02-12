export interface RatePlayerRequest {
  rating: number;
}

export interface PlayerRatingResponse {
  playerName: string;
  team: string;
  averageRating: number;
  ratingCount: number;
  myRating?: number;
}
