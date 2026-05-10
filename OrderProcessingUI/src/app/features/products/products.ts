import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { CurrencyPipe } from '@angular/common';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { Product } from '../../core/models/product.model';
import { AddProductDialogComponent } from './add-product-dialog';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    ReactiveFormsModule, CurrencyPipe,
    MatCardModule, MatButtonModule, MatIconModule, MatTableModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSnackBarModule, MatProgressSpinnerModule, MatChipsModule,
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Products</h1>
        <button mat-raised-button color="primary" (click)="openAddDialog()">
          <mat-icon>add</mat-icon> Add Product
        </button>
      </div>

      @if (loading()) {
        <div class="center"><mat-spinner></mat-spinner></div>
      } @else {
        <div class="products-grid">
          @for (product of products(); track product.id) {
            <mat-card class="product-card">
              <mat-card-header>
                <mat-card-title>{{ product.name }}</mat-card-title>
                <mat-card-subtitle>ID: {{ product.id }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <p class="price">{{ product.price | currency }}</p>
                <mat-chip [color]="product.stock > 0 ? 'primary' : 'warn'" highlighted>
                  {{ product.stock > 0 ? product.stock + ' in stock' : 'Out of stock' }}
                </mat-chip>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button color="primary"
                  [disabled]="product.stock === 0 || addingToCart() === product.id"
                  (click)="addToCart(product)">
                  @if (addingToCart() === product.id) {
                    <mat-spinner diameter="18"></mat-spinner>
                  } @else {
                    <mat-icon>add_shopping_cart</mat-icon>
                  }
                  Add to Cart
                </button>
                <button mat-icon-button color="accent" (click)="openStockDialog(product)" title="Update stock">
                  <mat-icon>edit</mat-icon>
                </button>
              </mat-card-actions>
            </mat-card>
          } @empty {
            <p class="empty">No products found.</p>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 1200px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
    .page-header h1 { margin: 0; }
    .products-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; }
    .product-card { height: 100%; }
    .price { font-size: 1.4rem; font-weight: 600; color: #3f51b5; margin: 8px 0; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .empty { text-align: center; color: #666; padding: 40px; }
  `],
})
export class ProductsComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  products = signal<Product[]>([]);
  loading = signal(false);
  addingToCart = signal<number | null>(null);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.productService.getAll().subscribe({
      next: (data) => { this.products.set(data); this.loading.set(false); },
      error: () => { this.snack.open('Failed to load products', 'Close', { duration: 3000 }); this.loading.set(false); },
    });
  }

  addToCart(product: Product) {
    const userId = this.auth.currentUser()?.id;
    if (!userId) {
      this.snack.open('Session expired — please log in again', 'Close', { duration: 4000 });
      return;
    }
    this.addingToCart.set(product.id);
    this.cartService.addItem(userId, { productId: product.id, quantity: 1 }).subscribe({
      next: () => {
        this.snack.open(`${product.name} added to cart`, 'View Cart', { duration: 3000 });
        this.addingToCart.set(null);
      },
      error: (err) => {
        this.snack.open(err.error?.message ?? 'Failed to add to cart', 'Close', { duration: 3000 });
        this.addingToCart.set(null);
      },
    });
  }

  openAddDialog() {
    const ref = this.dialog.open(AddProductDialogComponent, { width: '420px' });
    ref.afterClosed().subscribe(result => { if (result) this.load(); });
  }

  openStockDialog(product: Product) {
    const qty = prompt(`Update stock for "${product.name}" (current: ${product.stock}):`);
    if (qty === null || qty.trim() === '') return;
    const quantity = parseInt(qty, 10);
    if (isNaN(quantity)) { this.snack.open('Invalid quantity', 'Close', { duration: 2000 }); return; }
    this.productService.updateStock(product.id, { quantity }).subscribe({
      next: () => { this.snack.open('Stock updated', 'Close', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Failed to update stock', 'Close', { duration: 3000 }),
    });
  }
}
