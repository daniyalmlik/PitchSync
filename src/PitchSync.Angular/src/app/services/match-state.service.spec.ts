import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { MatchStateService } from './match-state.service';
import { ApiService } from './api.service';
import { SignalrService } from './signalr.service';
import { AuthService } from './auth.service';
import { MatchRoomResponse } from '../models/match.model';
import { MatchEventResponse } from '../models/event.model';
import { PlayerRatingResponse } from '../models/rating.model';

const makeRoom = (overrides: Partial<MatchRoomResponse> = {}): MatchRoomResponse => ({
  id: 'room-1',
  title: 'Test Room',
  homeTeam: 'Home FC',
  awayTeam: 'Away FC',
  kickoffTime: new Date().toISOString(),
  status: 'Upcoming',
  homeScore: 0,
  awayScore: 0,
  isPublic: true,
  createdByUserId: 'u1',
  createdAt: new Date().toISOString(),
  participants: [],
  events: [],
  homeLineup: [],
  awayLineup: [],
  ...overrides,
});

const makeEvent = (overrides: Partial<MatchEventResponse> = {}): MatchEventResponse => ({
  id: 'ev-1',
  minute: 42,
  eventType: 'Goal',
  postedByUserId: 'u1',
  postedByDisplayName: 'Alice',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('MatchStateService', () => {
  let service: MatchStateService;

  let mockApi: {
    getRoom: ReturnType<typeof vi.fn>;
    getRatings: ReturnType<typeof vi.fn>;
    postEvent: ReturnType<typeof vi.fn>;
  };

  let signalrSubjects: {
    eventPosted$: Subject<MatchEventResponse>;
    scoreUpdated$: Subject<{ roomId: string; homeScore: number; awayScore: number }>;
    presenceUpdate$: Subject<{ roomId: string; onlineUsers: any[] }>;
    statusChanged$: Subject<{ roomId: string; status: any }>;
    ratingsUpdated$: Subject<{ roomId: string; allRatings: PlayerRatingResponse[] }>;
    eventDeleted$: Subject<string>;
    participantJoined$: Subject<{ roomId: string; participant: any }>;
    participantLeft$: Subject<{ roomId: string; userId: string }>;
  };

  beforeEach(() => {
    signalrSubjects = {
      eventPosted$: new Subject(),
      scoreUpdated$: new Subject(),
      presenceUpdate$: new Subject(),
      statusChanged$: new Subject(),
      ratingsUpdated$: new Subject(),
      eventDeleted$: new Subject(),
      participantJoined$: new Subject(),
      participantLeft$: new Subject(),
    };

    mockApi = {
      getRoom: vi.fn().mockReturnValue(of(makeRoom())),
      getRatings: vi.fn().mockReturnValue(of([])),
      postEvent: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ApiService, useValue: mockApi },
        {
          provide: SignalrService,
          useValue: {
            connect: vi.fn().mockResolvedValue(undefined),
            disconnect: vi.fn().mockResolvedValue(undefined),
            ...signalrSubjects,
          },
        },
        {
          provide: AuthService,
          useValue: {
            getToken: vi.fn().mockReturnValue('token'),
            getCurrentUser: vi.fn().mockReturnValue({ id: 'u1', email: 'a@b.com', displayName: 'Alice' }),
            currentUser$: of(null),
          },
        },
      ],
    });

    service = TestBed.inject(MatchStateService);
  });

  it('loadRoom_FetchesRoomData_AndSetsRoom$', async () => {
    const rooms: (MatchRoomResponse | null)[] = [];
    service.currentRoom$.subscribe(r => rooms.push(r));

    await service.loadRoom('room-1');

    expect(mockApi.getRoom).toHaveBeenCalledWith('room-1');
    const last = rooms[rooms.length - 1];
    expect(last).not.toBeNull();
    expect(last!.id).toBe('room-1');
  });

  it('EventPosted_SignalAddsEvent', async () => {
    await service.loadRoom('room-1');

    const allEvents: MatchEventResponse[][] = [];
    service.events$.subscribe(e => allEvents.push([...e]));

    const newEvent = makeEvent({ id: 'ev-signal' });
    signalrSubjects.eventPosted$.next(newEvent);

    const last = allEvents[allEvents.length - 1];
    expect(last.some(e => e.id === 'ev-signal')).toBe(true);
  });

  it('ScoreUpdated_SignalUpdatesScore', async () => {
    await service.loadRoom('room-1');

    const scores: ({ homeScore: number; awayScore: number } | null)[] = [];
    service.score$.subscribe(s => scores.push(s));

    signalrSubjects.scoreUpdated$.next({ roomId: 'room-1', homeScore: 3, awayScore: 1 });

    const last = scores[scores.length - 1];
    expect(last).toEqual({ homeScore: 3, awayScore: 1 });
  });

  it('OptimisticPost_AddsTemp_ReplacesOnSuccess_RemovesOnError', async () => {
    await service.loadRoom('room-1');

    const eventSnapshots: MatchEventResponse[][] = [];
    service.events$.subscribe(e => eventSnapshots.push([...e]));

    // --- Success phase ---
    const realEvent = makeEvent({ id: 'real-1', minute: 10, eventType: 'Comment' });
    mockApi.postEvent.mockReturnValueOnce(of(realEvent));

    await service.postEventOptimistic({ minute: 10, eventType: 'Comment' });

    // A temp entry must have appeared in an intermediate snapshot
    const hadTemp = eventSnapshots.some(s => s.some(e => e.id.startsWith('temp-')));
    expect(hadTemp).toBe(true);

    // Final snapshot: real event present, no temp
    const afterSuccess = eventSnapshots[eventSnapshots.length - 1];
    expect(afterSuccess.some(e => e.id === 'real-1')).toBe(true);
    expect(afterSuccess.some(e => e.id.startsWith('temp-'))).toBe(false);

    // --- Error phase ---
    mockApi.postEvent.mockReturnValueOnce(throwError(() => new Error('network error')));

    const snapshotsBeforeError = eventSnapshots.length;
    await expect(service.postEventOptimistic({ minute: 20, eventType: 'Comment' })).rejects.toThrow('network error');

    // Temp was added during the error phase
    const errorPhase = eventSnapshots.slice(snapshotsBeforeError);
    expect(errorPhase.some(s => s.some(e => e.id.startsWith('temp-')))).toBe(true);

    // After rejection, temp removed
    const afterError = eventSnapshots[eventSnapshots.length - 1];
    expect(afterError.some(e => e.id.startsWith('temp-'))).toBe(false);
  });
});
