import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.getCurrentUserValue();
  const requiredRoles = route.data?.['roles'] as string[];

  if (!user || !requiredRoles || !requiredRoles.includes(user.role)) {
    router.navigate(['/dashboard']);
    return false;
  }

  return true;
};

