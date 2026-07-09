import { Injectable } from '@angular/core';
import { TokenPair, TokenStorageType } from '../models/Auth.models';

const ACCESS_TOKEN_KEY = 'auth_access_token';
const REFRESH_TOKEN_KEY = 'auth_refresh_token';
const STORAGE_TYPE_KEY = 'local';

/**
 * Інкапсулює роботу зі сховищем токенів.
 * Дозволяє на льоту обирати localStorage (persist between sessions)
 * або sessionStorage (токен живе, поки відкрита вкладка) — напр. чекбокс "Запам'ятати мене".
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private get storage(): Storage {
    const type = (localStorage.getItem(STORAGE_TYPE_KEY) as TokenStorageType) ?? 'local';
    return type === 'session' ? sessionStorage : localStorage;
  }

  setStorageType(type: TokenStorageType): void {
    // storage-type завжди тримаємо в localStorage, щоб пережити перезавантаження сторінки
    localStorage.setItem(STORAGE_TYPE_KEY, type);
  }

  setTokens(tokens: TokenPair): void {
    this.storage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    this.storage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  }

  getAccessToken(): string | null { 
    return this.storage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return this.storage.getItem(REFRESH_TOKEN_KEY);
  }

  hasTokens(): boolean {
    return !!this.getAccessToken() && !!this.getRefreshToken();
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  }
}