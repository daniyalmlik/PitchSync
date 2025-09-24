import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { catchError, timeout } from 'rxjs/operators';
import { of } from 'rxjs';
import { ApiService } from '../services/api.service';
import { MatchRoomSummary, PagedResult } from '../models/match.model';

export const matchesResolver: ResolveFn<PagedResult<MatchRoomSummary>> =
  () => inject(ApiService).getPublicRooms(1, 50)
    .pipe(
      timeout(8_000),
      catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 50, totalPages: 0, hasNext: false }))
    );
