import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';

const SKIP_PATHS = ['/api/auth/login', '/api/auth/register'];

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const shouldSkip = SKIP_PATHS.some(p => req.url.includes(p));
  if (shouldSkip) return next(req);

  const token = auth.getToken();
  if (!token) return next(req);

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
