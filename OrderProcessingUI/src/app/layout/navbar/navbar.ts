import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary">
      <span class="brand">Order Processing</span>
      <span class="spacer"></span>
      @if (auth.isLoggedIn()) {
        <a mat-button routerLink="/products" routerLinkActive="active-link">
          <mat-icon>inventory_2</mat-icon> Products
        </a>
        <a mat-button routerLink="/cart" routerLinkActive="active-link">
          <mat-icon>shopping_cart</mat-icon> Cart
        </a>
        <a mat-button routerLink="/checkout" routerLinkActive="active-link">
          <mat-icon>receipt_long</mat-icon> Checkout
        </a>
        <a mat-button routerLink="/transactions" routerLinkActive="active-link">
          <mat-icon>payment</mat-icon> Transactions
        </a>
        <span class="user-name">{{ auth.currentUser()?.name }}</span>
        <button mat-icon-button (click)="auth.logout()" title="Logout">
          <mat-icon>logout</mat-icon>
        </button>
      } @else {
        <a mat-button routerLink="/login">Login</a>
        <a mat-button routerLink="/register">Register</a>
      }
    </mat-toolbar>
  `,
  styles: [`
    .brand { font-size: 1.2rem; font-weight: 600; }
    .spacer { flex: 1 1 auto; }
    .active-link { background: rgba(255,255,255,0.15); border-radius: 4px; }
    .user-name { margin: 0 8px; font-size: 0.9rem; opacity: 0.85; }
    mat-icon { vertical-align: middle; margin-right: 4px; }
  `],
})
export class NavbarComponent {
  auth = inject(AuthService);
}
