import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then(m => m.RegisterComponent),
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/products').then(m => m.ProductsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart').then(m => m.CartComponent),
    canActivate: [authGuard],
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout').then(m => m.CheckoutComponent),
    canActivate: [authGuard],
  },
  {
    path: 'transactions',
    loadComponent: () => import('./features/transactions/transactions').then(m => m.TransactionsComponent),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: 'products' },
];
