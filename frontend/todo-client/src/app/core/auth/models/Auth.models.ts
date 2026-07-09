// === Базові сутності ===

export interface User {
  id: string;
  email: string;
  username: string;
  createdAt: string;
  avatarUrl?: string;
  totalScore: number;
}

export interface TokenPair {
  accessToken: string;
  refreshToken: string;
}

/** Стан автентифікації, який тримає AuthService */
export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

// === Login / Register ===

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
}

/** Бекенд лише повідомляє, що код надіслано на пошту */
export interface MessageResponse {
  message: string;
}

export interface VerifyEmailRequest {
  email: string;
  code: string;
}

export interface GoogleOAuthRequest {
  idToken: string;
}

/** Відповідь, що одразу містить токени + користувача (login, verify-email, google) */
export interface AuthResponse extends TokenPair {
  user: User;
}

// === Refresh ===

export interface RefreshRequest {
  accessToken: string;
  refreshToken: string;
}

export interface RefreshResponse extends TokenPair {}

// === Зміна паролю (двоетапна дія: запит коду -> підтвердження) ===

export interface ChangePasswordInitRequest {
  oldPassword: string;
  newPassword: string;
}

export interface ChangePasswordConfirmRequest {
  code: string;
}

// === Видалення акаунта (двоетапна дія: запит коду -> підтвердження) ===

export interface RemoveAccountInitRequest {
  password: string;
}

export interface RemoveAccountConfirmRequest {
  code: string;
}

/** Де саме зберігати токени */
export type TokenStorageType = 'local' | 'session';