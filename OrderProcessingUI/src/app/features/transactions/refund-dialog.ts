import { Component, inject, Inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PaymentService } from '../../core/services/payment.service';
import { Transaction } from '../../core/models/payment.model';

@Component({
  selector: 'app-refund-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Request Refund</h2>
    <mat-dialog-content>
      <p>Transaction: <strong>{{ data.transaction.transactionId }}</strong></p>
      <p>Original amount: <strong>\${{ data.transaction.amount }}</strong></p>
      <form [formGroup]="form" id="refundForm" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Refund Amount ($)</mat-label>
          <input matInput type="number" step="0.01" formControlName="amount" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Reason</mat-label>
          <textarea matInput formControlName="reason" rows="3"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="warn" form="refundForm" type="submit" [disabled]="form.invalid || saving">
        {{ saving ? 'Processing...' : 'Request Refund' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width { width: 100%; margin-bottom: 8px; }`],
})
export class RefundDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private paymentService = inject(PaymentService);
  private dialogRef = inject(MatDialogRef<RefundDialogComponent>);
  private snack = inject(MatSnackBar);

  saving = false;
  form = this.fb.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    reason: ['', Validators.required],
  });

  constructor(@Inject(MAT_DIALOG_DATA) public data: { transaction: Transaction }) {}

  ngOnInit() {
    this.form.patchValue({ amount: this.data.transaction.amount });
  }

  submit() {
    if (this.form.invalid) return;
    this.saving = true;
    this.paymentService.refund({
      transactionId: this.data.transaction.transactionId,
      amount: this.form.value.amount!,
      reason: this.form.value.reason!,
    }).subscribe({
      next: (res) => {
        this.snack.open(res.success ? 'Refund processed' : res.message, 'Close', { duration: 3000 });
        this.dialogRef.close(res.success);
      },
      error: () => { this.snack.open('Refund failed', 'Close', { duration: 3000 }); this.saving = false; },
    });
  }
}
