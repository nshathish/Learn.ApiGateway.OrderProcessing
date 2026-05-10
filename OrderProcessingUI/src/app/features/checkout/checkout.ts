import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperModule } from '@angular/material/stepper';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CheckoutService } from '../../core/services/checkout.service';
import { PaymentService } from '../../core/services/payment.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { CheckoutPageResponse } from '../../core/models/checkout.model';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [
    ReactiveFormsModule, CurrencyPipe, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatStepperModule, MatDividerModule,
    MatSnackBarModule, MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-container">
      <h1>Checkout</h1>

      @if (loading()) {
        <div class="center"><mat-spinner></mat-spinner></div>
      } @else if (data()) {
        <mat-stepper orientation="vertical" [linear]="true" #stepper>

          <!-- Step 1: Order Summary -->
          <mat-step label="Order Summary" [completed]="!!data()?.cart?.items?.length">
            <mat-card class="step-card">
              <mat-card-content>
                <h3>Items in Cart</h3>
                @for (item of enrichedCartItems(); track item.productId) {
                  <div class="order-item-details">
                    <div class="order-item-left">
                      <div class="order-item-name">{{ item.productName }}</div>
                      <div class="order-item-price-qty">
                        <span>{{ item.price | currency }}</span>
                        <span> × {{ item.quantity }}</span>
                      </div>
                    </div>
                    <div class="order-item-subtotal">{{ item.price * item.quantity | currency }}</div>
                  </div>
                } @empty {
                  <p>Your cart is empty. <a href="/products">Shop now</a></p>
                }
                <mat-divider></mat-divider>
                <div class="order-total">
                  <strong>Total:</strong>
                  <strong class="total-amount">{{ cartTotal() | currency }}</strong>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" matStepperNext [disabled]="!data()?.cart?.items?.length">
                  Continue
                </button>
              </mat-card-actions>
            </mat-card>
          </mat-step>

          <!-- Step 2: Shipping Info -->
          <mat-step label="Shipping Info" [stepControl]="shippingForm">
            <mat-card class="step-card">
              <mat-card-content>
                <form [formGroup]="shippingForm">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Delivery Address</mat-label>
                    <input matInput formControlName="address" />
                    <mat-error>Address is required</mat-error>
                  </mat-form-field>
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="primary" matStepperNext [disabled]="shippingForm.invalid">Continue</button>
              </mat-card-actions>
            </mat-card>
          </mat-step>

          <!-- Step 3: Payment -->
          <mat-step label="Payment" [stepControl]="paymentForm">
            <mat-card class="step-card">
              <mat-card-content>
                <form [formGroup]="paymentForm">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Payment Method</mat-label>
                    <mat-select formControlName="paymentMethodId">
                      @for (method of data()!.paymentMethods; track method.id) {
                        <mat-option [value]="method.id">
                          {{ method.name }} ({{ method.processingFee | currency }} fee)
                        </mat-option>
                      }
                    </mat-select>
                    <mat-error>Select a payment method</mat-error>
                  </mat-form-field>

                  @if (isCardPayment()) {
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Card Number (16 digits)</mat-label>
                      <input matInput formControlName="cardNumber" maxlength="16" />
                    </mat-form-field>
                    <div class="card-row">
                      <mat-form-field appearance="outline">
                        <mat-label>Expiry (MM/YY)</mat-label>
                        <input matInput formControlName="cardExpiry" placeholder="12/28" />
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>CVV</mat-label>
                        <input matInput formControlName="cardCvv" maxlength="4" type="password" />
                      </mat-form-field>
                    </div>
                  }
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="accent" (click)="placeOrder(stepper)" [disabled]="paymentForm.invalid || placing()">
                  @if (placing()) { <mat-spinner diameter="20"></mat-spinner> } @else { Place Order }
                </button>
              </mat-card-actions>
            </mat-card>
          </mat-step>

          <!-- Step 4: Confirmation -->
          <mat-step label="Confirmation">
            <mat-card class="step-card success-card">
              <mat-card-content>
                <mat-icon class="success-icon">check_circle</mat-icon>
                <h2>Order Placed!</h2>
                @if (txnId()) {
                  <p>Transaction ID: <strong>{{ txnId() }}</strong></p>
                }
                <p>Total charged: <strong>{{ txnTotal() | currency }}</strong></p>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" routerLink="/transactions">View Transactions</button>
              </mat-card-actions>
            </mat-card>
          </mat-step>

        </mat-stepper>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 700px; margin: 0 auto; }
    h1 { margin-bottom: 24px; }
    .step-card { margin: 16px 0; }
    .full-width { width: 100%; }

    h3 { margin-top: 0; margin-bottom: 16px; font-size: 18px; }

    .order-item-details {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 0;
      border-bottom: 1px solid #f0f0f0;
      gap: 16px;
    }

    .order-item-details:last-of-type {
      border-bottom: none;
    }

    .order-item-left {
      flex: 1;
      min-width: 150px;
    }

    .order-item-name {
      font-weight: 600;
      color: #333;
      margin-bottom: 4px;
      font-size: 14px;
    }

    .order-item-price-qty {
      font-size: 13px;
      color: #666;
    }

    .order-item-price-qty span {
      margin-right: 4px;
    }

    .order-item-subtotal {
      min-width: 70px;
      text-align: right;
      font-weight: 600;
      color: #3f51b5;
      font-size: 14px;
    }

    mat-divider {
      margin: 12px 0 !important;
    }

    .order-total {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 0;
      margin-top: 8px;
      font-size: 16px;
    }

    .total-amount {
      font-size: 18px;
      color: #3f51b5;
    }

    .total-row { margin-top: 8px; font-size: 1.1rem; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .card-row { display: flex; gap: 16px; }
    .card-row mat-form-field { flex: 1; }
    .success-card mat-card-content { text-align: center; padding: 24px; }
    .success-icon { font-size: 64px; width: 64px; height: 64px; color: #4caf50; }
  `],
})
export class CheckoutComponent implements OnInit {
  private checkoutService = inject(CheckoutService);
  private paymentService = inject(PaymentService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  data = signal<CheckoutPageResponse | null>(null);
  loading = signal(false);
  placing = signal(false);
  txnId = signal<string | null>(null);
  txnTotal = signal<number>(0);

  shippingForm = this.fb.group({
    address: ['', Validators.required],
  });

  paymentForm = this.fb.group({
    paymentMethodId: [null as number | null, Validators.required],
    cardNumber: [''],
    cardExpiry: [''],
    cardCvv: [''],
  });

  enrichedCartItems() {
    const items = this.data()?.cart?.items ?? [];
    const products = this.data()?.products ?? [];
    return items.map(item => {
      const itemId = (item as any).productId ?? (item as any).id;
      const product = products.find(p => p.id === itemId);
      return {
        productId: itemId,
        productName: (item as any).productName || product?.name || 'Unknown',
        price: (item as any).price || product?.price || 0,
        quantity: item.quantity,
      };
    });
  }

  isCardPayment() {
    const methodId = this.paymentForm.value.paymentMethodId;
    const method = this.data()?.paymentMethods.find(m => m.id === methodId);
    return method?.code === 'credit_card';
  }

  cartTotal() {
    return this.enrichedCartItems().reduce((s, i) => s + i.price * i.quantity, 0);
  }

  ngOnInit() {
    const user = this.auth.currentUser();
    if (!user) return;
    this.shippingForm.patchValue({ address: user.address });
    this.loading.set(true);
    this.checkoutService.getCheckoutPage(user.id).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: () => { this.snack.open('Failed to load checkout', 'Close', { duration: 3000 }); this.loading.set(false); },
    });
  }

   placeOrder(stepper: any) {
     const user = this.auth.currentUser();
     if (!user) return;

     this.placing.set(true);

     // Fetch the latest cart data before processing payment
     this.cartService.getCart(user.id).subscribe({
       next: (cart) => {
         const d = this.data();
         if (!d) { this.placing.set(false); return; }

         const orderId = crypto.randomUUID();
         const amount = this.calculateOrderTotalFromCart(cart);

         console.log('Order ID:', orderId);
         console.log('User ID:', user.id);
         console.log('Final Amount:', amount);

         this.paymentService.processPayment({
           orderId,
           userId: user.id,
           amount,
           currency: 'USD',
           paymentMethodId: this.paymentForm.value.paymentMethodId!,
           cardNumber: this.paymentForm.value.cardNumber || undefined,
           cardExpiry: this.paymentForm.value.cardExpiry || undefined,
           cardCvv: this.paymentForm.value.cardCvv || undefined,
           billingAddress: this.shippingForm.value.address || undefined,
         }).subscribe({
           next: (res) => {
             this.txnId.set(res.transactionId);
             this.txnTotal.set(res.totalAmount);
             this.placing.set(false);
             stepper.next();
           },
           error: (err) => {
             this.snack.open(err.error?.message ?? 'Payment failed', 'Close', { duration: 4000 });
             this.placing.set(false);
           },
         });
       },
       error: () => {
         this.snack.open('Failed to load cart data', 'Close', { duration: 3000 });
         this.placing.set(false);
       },
     });
   }

    private calculateOrderTotalFromCart(cart: any): number {
      const items = cart?.items ?? [];
      console.log('Cart from API:', cart);
      console.log('Items from cart:', items);

      const total = items.reduce((sum: number, item: any) => {
        const price = parseFloat(item.price) || 0;
        const quantity = parseInt(item.quantity) || 0;
        const itemTotal = price * quantity;
        console.log(`Item: ${item.productName}, Price: ${price}, Qty: ${quantity}, Subtotal: ${itemTotal}`);
        return sum + itemTotal;
      }, 0);

      console.log('Total calculated from cart:', total);
      return total;
    }
  }
