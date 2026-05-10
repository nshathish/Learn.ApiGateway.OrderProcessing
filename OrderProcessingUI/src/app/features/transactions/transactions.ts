import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, SlicePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { PaymentService } from '../../core/services/payment.service';
import { AuthService } from '../../core/services/auth.service';
import { Transaction, PaymentSummary } from '../../core/models/payment.model';
import { RefundDialogComponent } from './refund-dialog';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [
    CurrencyPipe, DatePipe, SlicePipe,
    MatCardModule, MatButtonModule, MatIconModule, MatTableModule,
    MatChipsModule, MatDialogModule, MatSnackBarModule, MatProgressSpinnerModule,
    MatTabsModule,
  ],
  template: `
    <div class="page-container">
      <h1>Payment History</h1>

      @if (loading()) {
        <div class="center"><mat-spinner></mat-spinner></div>
      } @else {
        <mat-tab-group>

          <mat-tab label="Transactions">
            @if (!transactions().length) {
              <p class="empty">No transactions found.</p>
            } @else {
              <table mat-table [dataSource]="transactions()" class="full-width">
                <ng-container matColumnDef="date">
                  <th mat-header-cell *matHeaderCellDef>Date</th>
                  <td mat-cell *matCellDef="let t">{{ t.createdAt | date:'short' }}</td>
                </ng-container>
                <ng-container matColumnDef="orderId">
                  <th mat-header-cell *matHeaderCellDef>Order ID</th>
                  <td mat-cell *matCellDef="let t" class="mono">{{ t.orderId | slice:0:8 }}...</td>
                </ng-container>
                <ng-container matColumnDef="method">
                  <th mat-header-cell *matHeaderCellDef>Method</th>
                  <td mat-cell *matCellDef="let t">{{ t.paymentMethod }}</td>
                </ng-container>
                <ng-container matColumnDef="amount">
                  <th mat-header-cell *matHeaderCellDef>Amount</th>
                  <td mat-cell *matCellDef="let t">{{ t.amount | currency:t.currency }}</td>
                </ng-container>
                <ng-container matColumnDef="status">
                  <th mat-header-cell *matHeaderCellDef>Status</th>
                  <td mat-cell *matCellDef="let t">
                    <mat-chip [color]="statusColor(t.status)" highlighted>{{ t.status }}</mat-chip>
                  </td>
                </ng-container>
                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef></th>
                  <td mat-cell *matCellDef="let t">
                    @if (t.status === 'completed') {
                      <button mat-button color="warn" (click)="openRefund(t)">Refund</button>
                    }
                  </td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="columns"></tr>
                <tr mat-row *matRowDef="let row; columns: columns;"></tr>
              </table>
            }
          </mat-tab>

          <mat-tab label="Summary">
            @if (summary()) {
              <div class="summary-grid">
                <mat-card class="summary-stat">
                  <mat-card-content>
                    <p class="stat-label">Total Spent</p>
                    <p class="stat-value">{{ summary()!.totalSpent | currency }}</p>
                  </mat-card-content>
                </mat-card>
                <mat-card class="summary-stat">
                  <mat-card-content>
                    <p class="stat-label">Total Refunded</p>
                    <p class="stat-value refund">{{ summary()!.totalRefunded | currency }}</p>
                  </mat-card-content>
                </mat-card>
                <mat-card class="summary-stat">
                  <mat-card-content>
                    <p class="stat-label">Net Spent</p>
                    <p class="stat-value">{{ summary()!.netSpent | currency }}</p>
                  </mat-card-content>
                </mat-card>
                <mat-card class="summary-stat">
                  <mat-card-content>
                    <p class="stat-label">Transactions</p>
                    <p class="stat-value">{{ summary()!.transactionCount }}</p>
                  </mat-card-content>
                </mat-card>
              </div>
            }
          </mat-tab>

        </mat-tab-group>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 1100px; margin: 0 auto; }
    h1 { margin-bottom: 24px; }
    .full-width { width: 100%; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .empty { text-align: center; color: #666; padding: 40px; }
    .mono { font-family: monospace; font-size: 0.85rem; }
    .summary-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; padding: 24px 0; }
    .summary-stat mat-card-content { text-align: center; padding: 16px; }
    .stat-label { font-size: 0.85rem; color: #666; margin: 0 0 8px; }
    .stat-value { font-size: 1.6rem; font-weight: 700; color: #3f51b5; margin: 0; }
    .stat-value.refund { color: #f44336; }
  `],
})
export class TransactionsComponent implements OnInit {
  private paymentService = inject(PaymentService);
  private auth = inject(AuthService);
  private snack = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  transactions = signal<Transaction[]>([]);
  summary = signal<PaymentSummary | null>(null);
  loading = signal(false);
  columns = ['date', 'orderId', 'method', 'amount', 'status', 'actions'];

  statusColor(status: string): 'primary' | 'accent' | 'warn' {
    if (status === 'completed') return 'primary';
    if (status === 'refunded') return 'accent';
    return 'warn';
  }

  ngOnInit() {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;
    this.loading.set(true);
    this.paymentService.getUserTransactions(userId).subscribe({
      next: (txns) => { this.transactions.set(txns); this.loading.set(false); },
      error: () => { this.snack.open('Failed to load transactions', 'Close', { duration: 3000 }); this.loading.set(false); },
    });
    this.paymentService.getSummary(userId).subscribe({
      next: (s) => this.summary.set(s),
      error: () => {},
    });
  }

  openRefund(txn: Transaction) {
    const ref = this.dialog.open(RefundDialogComponent, {
      width: '420px',
      data: { transaction: txn },
    });
    ref.afterClosed().subscribe(done => { if (done) this.ngOnInit(); });
  }
}
