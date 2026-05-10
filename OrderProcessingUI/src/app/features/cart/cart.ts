import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { Cart } from '../../core/models/cart.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
    RouterLink, CurrencyPipe,
    MatCardModule, MatButtonModule, MatIconModule,
    MatSnackBarModule, MatProgressSpinnerModule, MatDividerModule,
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Shopping Cart</h1>
      </div>

      @if (loading()) {
        <div class="center"><mat-spinner></mat-spinner></div>
      } @else if (!cart()?.items?.length) {
        <mat-card class="empty-card">
          <mat-card-content>
            <mat-icon class="empty-icon">shopping_cart</mat-icon>
            <p>Your cart is empty.</p>
            <a mat-raised-button color="primary" routerLink="/products">Browse Products</a>
          </mat-card-content>
        </mat-card>
      } @else {
        <mat-card class="cart-card">
          <mat-card-header>
            <mat-card-title>Items in Cart</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="items-list">
              @for (item of cart()?.items; track item.productId) {
                <div class="cart-item">
                  <div class="item-left">
                    <div class="item-name">{{ item.productName }}</div>
                    <div class="item-price-qty">
                      <span>{{ item.price | currency }}</span>
                      <span> × {{ item.quantity }}</span>
                    </div>
                  </div>
                  <div class="item-right">
                    <div class="item-subtotal">{{ item.price * item.quantity | currency }}</div>
                    <button mat-icon-button color="warn" (click)="removeItem(item.productId)" title="Remove item">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                </div>
              }
            </div>
          </mat-card-content>
          <mat-divider></mat-divider>
          <mat-card-content class="total-section">
            <div class="total-row">
              <span class="total-label">Total</span>
              <span class="total-amount">{{ cartTotal() | currency }}</span>
            </div>
          </mat-card-content>
          <mat-card-actions class="action-buttons">
            <a mat-raised-button routerLink="/products">
              <mat-icon>arrow_back</mat-icon> Continue Shopping
            </a>
            <a mat-raised-button color="primary" routerLink="/checkout">
              <mat-icon>receipt_long</mat-icon> Proceed to Checkout
            </a>
          </mat-card-actions>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 800px; margin: 0 auto; }
    .page-header { margin-bottom: 24px; }
    .page-header h1 { margin: 0; font-size: 28px; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .empty-card mat-card-content { text-align: center; padding: 40px; }
    .empty-icon { font-size: 64px; width: 64px; height: 64px; color: #ccc; }
    .cart-card mat-card-header { padding: 16px; border-bottom: 1px solid #e0e0e0; }
    .items-list { padding: 16px 0; }
    .cart-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      border-bottom: 1px solid #f0f0f0;
      gap: 16px;
    }
    .cart-item:last-child {
      border-bottom: none;
    }
    .item-left {
      flex: 1;
      min-width: 200px;
    }
    .item-name {
      font-weight: 600;
      color: #333;
      margin-bottom: 8px;
      font-size: 16px;
    }
    .item-price-qty {
      font-size: 14px;
      color: #666;
    }
    .item-price-qty span {
      margin-right: 8px;
    }
    .item-right {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 16px;
      min-width: 140px;
    }
    .item-subtotal {
      text-align: right;
      font-weight: 600;
      color: #3f51b5;
      font-size: 16px;
      min-width: 80px;
    }
    .total-section { padding: 16px; }
    .total-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 0;
    }
    .total-label { font-size: 16px; font-weight: 500; }
    .total-amount { font-size: 24px; font-weight: 700; color: #3f51b5; }
    .action-buttons {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      padding: 16px;
    }
    .action-buttons a {
      flex: 1;
      max-width: 250px;
    }
    @media (max-width: 600px) {
      .cart-item {
        flex-direction: column;
        align-items: flex-start;
      }
      .item-right {
        width: 100%;
        justify-content: space-between;
      }
      .item-subtotal {
        text-align: right;
      }
      .action-buttons {
        flex-direction: column;
      }
      .action-buttons a {
        max-width: 100%;
      }
    }
  `],
})
export class CartComponent implements OnInit {
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);

  cart = signal<Cart | null>(null);
  loading = signal(false);

  cartTotal() {
    return this.cart()?.items?.reduce((sum, i) => sum + i.price * i.quantity, 0) ?? 0;
  }

  ngOnInit() {
    this.load();
  }

  load() {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;
    this.loading.set(true);
    this.cartService.getCart(userId).subscribe({
      next: (data) => { this.cart.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }

  removeItem(productId: number) {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;
    this.cartService.removeItem(userId, productId).subscribe({
      next: (updated) => { this.cart.set(updated); this.snack.open('Item removed', 'Close', { duration: 2000 }); },
      error: () => this.snack.open('Failed to remove item', 'Close', { duration: 3000 }),
    });
  }
}
