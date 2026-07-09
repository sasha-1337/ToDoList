import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import {environment} from "../../../../environments/environment";
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';

let isRefreshing = false;
const refreshedAccessToken$ = new BehaviorSubject<string | null>(null);

const API_BASE = environment.authUrlHTTP + '/api/auth';

/**
 * ТІЛЬКИ ці ендпоінти не потребують токена і не мають тригерити refresh на 401
 * (бо там 401 означає "невірний пароль/код", а не "токен прострочений").
 * УСЕ інше під /api/auth/... (me, change-password, remove-account, logout)
 * потребує Authorization-заголовка — саме це раніше й ламало /me після reload.
 */
const PUBLIC_AUTH_ENDPOINTS = [
  `${API_BASE}/login`,
  `${API_BASE}/register`,
  `${API_BASE}/verify-email`,
  `${API_BASE}/google-login-oauth`,
  `${API_BASE}/refresh-token`,
];

function isPublicAuthEndpoint(url: string): boolean {
  return PUBLIC_AUTH_ENDPOINTS.some((endpoint) => url.startsWith(endpoint));
}

function withAuthHeader(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(Injector);
  const tokenStorage = inject(TokenStorageService);

  if (isPublicAuthEndpoint(req.url)) {
    return next(req);
  }

  const authorizedReq = withAuthHeader(req, tokenStorage.getAccessToken());

  return next(authorizedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        const authService = injector.get(AuthService);

        return handle401(authorizedReq, next, authService, tokenStorage);
      }
      return throwError(() => error);
    }),
  );
};

function handle401(
  originalReq: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  tokenStorage: TokenStorageService,
): Observable<any> {

  if (!isRefreshing) {
    isRefreshing = true;

    refreshedAccessToken$.next(null);

    return authService.refreshToken().pipe(
      switchMap((tokens) => {
        isRefreshing = false;

        refreshedAccessToken$.next(tokens.accessToken);
        return next(withAuthHeader(originalReq, tokens.accessToken));
      }),
      catchError((refreshError) => {
        isRefreshing = false;
        refreshedAccessToken$.next(null);
        authService.forceLogout();
        return throwError(() => refreshError);
      }),
    );
  }

  return refreshedAccessToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(withAuthHeader(originalReq, token))),
  );
}