import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog } from '@angular/material/dialog';

import { AuthService } from '../../auth/services/auth.service';
import { ProfileInfoDialogComponent } from '../profile-info-dialog/profile-info-dialog.component';
import { ChangePasswordDialogComponent } from '../change-password-dialog/change-password-dialog.component';
import { RemoveAccountDialogComponent } from '../remove-account-dialog/remove-account-dialog.component';

/**
 * Кнопка-шестерня з випадним меню налаштувань профілю.
 * Кладеться в кут контейнера профілю користувача (напр. в сайдбарі).
 * Сама читає AuthService.user() — нічого передавати ззовні не треба.
 */
@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [CommonModule, MatMenuModule, MatIconModule, MatButtonModule, MatTooltipModule, MatDividerModule],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  openProfileInfo(): void {
    this.dialog.open(ProfileInfoDialogComponent, { width: '360px' });
  }

  openChangePassword(): void {
    // Сам діалог всередині проведе користувача через крок "код з пошти" (Strategy-патерн бекенда)
    this.dialog.open(ChangePasswordDialogComponent, { width: '420px' });
  }

  openRemoveAccount(): void {
    // Так само двоетапно: пароль -> код з пошти. Навігація на /login відбувається всередині діалогу.
    this.dialog.open(RemoveAccountDialogComponent, { width: '420px' });
  }

  logout(): void {
    // AuthService.logout() гарантовано завершується успіхом навіть при недоступному бекенді
    // (там catchError -> of(void 0)), тож subscribe().next спрацює завжди.
    this.authService.logout().subscribe(() => this.router.navigateByUrl('/login'));
  }
}