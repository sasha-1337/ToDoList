import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, finalize } from 'rxjs';
import { environment } from '../../../../environments/environment';

import { TokenStorageService } from './token-storage.service';
import { UserScoreStore } from '../../state/user-score.store';
import {
  AuthResponse,
  ChangePasswordInitRequest,
  GoogleOAuthRequest,
  LoginRequest,
  MessageResponse,
  RefreshRequest,
  RefreshResponse,
  RegisterRequest,
  RemoveAccountInitRequest,
  TokenPair,
  User,
  VerifyEmailRequest,
} from '../models/Auth.models';

const API_BASE = environment.authUrlHTTP + '/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly scoreStore = inject(UserScoreStore);

  // === Стан користувача (Signals) ===
  private readonly _user = signal<User | null>(null);
  private readonly _isLoading = signal<boolean>(false);

  /** Публічний readonly-доступ до користувача */
  readonly user = this._user.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  /** Похідний стан: чи є валідна сесія прямо зараз */
  readonly isAuthenticated = computed(() => this._user() !== null && this.tokenStorage.hasTokens());

  constructor() {
    // При старті додатку, якщо в сховищі є токени — намагаємось відновити профіль.
    // Викликати це варто ще й з APP_INITIALIZER, щоб дочекатись перед першим рендером роутів.
    if (this.tokenStorage.hasTokens()) {
      this.restoreSession().subscribe();
    }
  }

  // ============================================================
  // ==================  ПУБЛІЧНІ HTTP-МЕТОДИ  ===================
  // ============================================================

  /** Логін email/пароль. Одразу оновлює стан користувача. */
  login(email: string, password: string): Observable<AuthResponse> {
    this._isLoading.set(true);
    return this.http
      .post<AuthResponse>(`${API_BASE}/login`, { email, password } satisfies LoginRequest)
      .pipe(
        tap((res) => this.setSession(res)),
        finalize(() => this._isLoading.set(false)),
      );
  }

  /**
   * Реєстрація. Бекенд лише надсилає код підтвердження на пошту.
   * Стан користувача НЕ змінюється — логін відбудеться в verifyEmail().
   */
  register(email: string, username: string, password: string, confirmPassword: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${API_BASE}/register`, {
      email,
      username,
      password,
      confirmPassword,
    } satisfies RegisterRequest);
  }

  /**
   * Підтвердження email кодом. Завершує реєстрацію і одразу авторизує
   * користувача — окремий виклик login() не потрібен.
   */
  verifyEmail(email: string, code: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE}/verify-email`, { email, code } satisfies VerifyEmailRequest)
      .pipe(tap((res) => this.setSession(res)));
  }

  /** Вхід через Google. Так само одразу авторизує. */
  googleOAuth(idToken: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE}/google-login-oauth`, { idToken } satisfies GoogleOAuthRequest)
      .pipe(tap((res) => this.setSession(res)));
  }

  /**
   * Вихід. Повідомляємо бекенд, щоб він інвалідував Refresh Token,
   * але локальний стан чистимо в будь-якому разі (навіть якщо запит впав).
   */
  logout(): Observable<void> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    return this.http.post<void>(`${API_BASE}/logout`, { refreshToken }).pipe(
      catchError(() => of(void 0)), // бекенд недоступний / токен вже прострочений — все одно логаутимось локально
      finalize(() => this.clearSession()),
    );
  }

  /** Синхронний "жорсткий" логаут — використовується інтерцептором при провалі refresh. */
  forceLogout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  // --- Зміна паролю: двоетапна дія (Strategy: init -> код на пошту -> confirm) ---

  /** ініціює зміну паролю, бекенд шле код на пошту користувача. */
  requestChangePassword(oldPassword: string, newPassword: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${API_BASE}/change-password`, {
      oldPassword,
      newPassword,
    } satisfies ChangePasswordInitRequest);
  }

  // --- Видалення акаунта: аналогічна двоетапна дія ---

  /** ініціює видалення акаунта, бекенд шле код на пошту. */
  requestRemoveAccount(password: string): Observable<MessageResponse> {
    const body: RemoveAccountInitRequest = { password };
    return this.http.delete<MessageResponse>(`${API_BASE}/remove-account`, {
      body,
    });
  }

  // ============================================================
  // ===============  ВІДНОВЛЕННЯ ТА REFRESH  ====================
  // ============================================================

  /**
   * Викликається інтерцептором при 401. НЕ використовує сам себе рекурсивно
   * і НЕ проходить через AuthInterceptor (ендпоінти /api/Auth/* виключені).
   */
  refreshToken(): Observable<RefreshResponse> {
    const body: RefreshRequest = {
      accessToken: this.tokenStorage.getAccessToken() ?? '',
      refreshToken: this.tokenStorage.getRefreshToken() ?? '',
    };
    return this.http
      .post<RefreshResponse>(`${API_BASE}/refresh-token`, body)
      .pipe(tap((tokens) => this.tokenStorage.setTokens(tokens)));
  }

  /** Дістає профіль користувача за наявним access token (напр. після перезавантаження сторінки). */
  private restoreSession(): Observable<AuthResponse | null> {
    this._isLoading.set(true);
    return this.http.get<AuthResponse>(`${API_BASE}/me`).pipe(
      tap((response) => {
          this.scoreStore.set(response.user.totalScore);
          this._user.set(response.user);
      }),
      catchError(() => {
        // access token протух і сам собою не поновився (наприклад SSR / перший рендер) —
        // інтерцептор в звичайному запиті сам зробить refresh; тут просто чистимо стан
        this.clearSession();
        return of(null);
      }),
      finalize(() => this._isLoading.set(false)),
    );
  }


  getAccessToken(): string | null {
    return this.tokenStorage.getAccessToken();
  }

  getRefreshToken(): string | null {
    return this.tokenStorage.getRefreshToken();
  }

  // ============================================================
  // =====================  ПРИВАТНІ ХЕЛПЕРИ  =====================
  // ============================================================

  private setSession(res: AuthResponse): void {
    const tokens: TokenPair = { accessToken: res.accessToken, refreshToken: res.refreshToken };
    this.tokenStorage.setTokens(tokens);
    this._user.set(res.user);
  }

  private clearSession(): void {
    this.tokenStorage.clear();
    this._user.set(null);
  }
}
