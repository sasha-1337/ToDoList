import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { AuthService } from '../../auth/services/auth.service';
import { UserScoreStore } from '../../state/user-score.store';


@Component({
  selector: 'app-profile-info-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './profile-info-dialog.component.html',
})
export class ProfileInfoDialogComponent {
  protected readonly authService = inject(AuthService);
  protected readonly scoreStore = inject(UserScoreStore);
  readonly dialogRef = inject(MatDialogRef<ProfileInfoDialogComponent>);
}