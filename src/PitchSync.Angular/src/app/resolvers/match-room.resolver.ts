import { inject } from '@angular/core';
import { ResolveFn, Router } from '@angular/router';
import { MatchStateService } from '../services/match-state.service';

export const matchRoomResolver: ResolveFn<void> =
  (route) => {
    const matchState = inject(MatchStateService);
    const router = inject(Router);
    return matchState.loadRoom(route.params['id'] as string)
      .catch(() => { router.navigate(['/matches']); });
  };
