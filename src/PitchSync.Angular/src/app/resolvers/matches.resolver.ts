import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, timeout } from 'rxjs/operators';
import { of } from 'rxjs';
import { ApiService } from '../services/api.service';
import { MatchRoomSummary } from '../models/match.model';

export const matchesResolver: ResolveFn<MatchRoomSummary[]> =
  () => inject(ApiService).getPublicRooms(1, 50)
    .pipe(timeout(8_000), catchError(() => of([])));
