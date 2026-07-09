import { Injectable, signal } from '@angular/core';

/**
 * Мінімальний shared-стан, окремий від AuthService.user,
 * щоб TaskService міг оновлювати рахунок одразу після PATCH-запиту,
 * не змушуючи весь додаток перезапитувати профіль користувача.
 */
@Injectable({ providedIn: 'root' })
export class UserScoreStore {
  private readonly _totalScore = signal<number | null>(null);
  readonly totalScore = this._totalScore.asReadonly();

  set(value: number): void {
    this._totalScore.set(value);
  }
}