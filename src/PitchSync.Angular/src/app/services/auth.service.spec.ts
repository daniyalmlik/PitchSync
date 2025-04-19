import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { UserInfo } from '../models/user.model';

const TOKEN_KEY = 'pitchsync_token';
const EXPIRES_KEY = 'pitchsync_expires';

// Minimal JWT: header.base64(payload).sig
const makeToken = (payload: object) =>
  'header.' + btoa(JSON.stringify(payload)) + '.sig';

const FAKE_TOKEN = makeToken({ sub: 'u1', email: 'a@b.com', display_name: 'Alice' });
const FUTURE_EXPIRES = new Date(Date.now() + 3_600_000).toISOString();
const PAST_EXPIRES = new Date(Date.now() - 1_000).toISOString();

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('login_StoresTokenInLocalStorage_OnSuccess', () => {
    service.login('a@b.com', 'pass').subscribe();

    const req = httpMock.expectOne('http://localhost:5000/api/auth/login');
    req.flush({ token: FAKE_TOKEN, expiresAt: FUTURE_EXPIRES, user: null });

    expect(localStorage.getItem(TOKEN_KEY)).toBe(FAKE_TOKEN);
    expect(localStorage.getItem(EXPIRES_KEY)).toBe(FUTURE_EXPIRES);
  });

  it('logout_ClearsToken', () => {
    localStorage.setItem(TOKEN_KEY, FAKE_TOKEN);
    localStorage.setItem(EXPIRES_KEY, FUTURE_EXPIRES);

    service.logout();

    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(localStorage.getItem(EXPIRES_KEY)).toBeNull();
  });

  it('isAuthenticated_ReturnsFalse_WhenTokenExpired', () => {
    localStorage.setItem(TOKEN_KEY, FAKE_TOKEN);
    localStorage.setItem(EXPIRES_KEY, PAST_EXPIRES);

    expect(service.isAuthenticated()).toBe(false);
  });

  it('currentUser$_EmitsUserInfo_AfterLogin', () => {
    const emissions: (UserInfo | null)[] = [];
    service.currentUser$.subscribe(u => emissions.push(u));

    service.login('a@b.com', 'pass').subscribe();

    const req = httpMock.expectOne('http://localhost:5000/api/auth/login');
    req.flush({ token: FAKE_TOKEN, expiresAt: FUTURE_EXPIRES, user: null });

    const lastEmission = emissions[emissions.length - 1];
    expect(lastEmission).not.toBeNull();
    expect(lastEmission!.email).toBe('a@b.com');
    expect(lastEmission!.displayName).toBe('Alice');
  });
});
