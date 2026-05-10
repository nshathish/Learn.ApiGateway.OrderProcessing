import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-add-product-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Add New Product</h2>
    <mat-dialog-content>
      <form [formGroup]="form" id="addProductForm" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Product Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Price ($)</mat-label>
          <input matInput type="number" step="0.01" formControlName="price" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Initial Stock</mat-label>
          <input matInput type="number" formControlName="stock" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" form="addProductForm" type="submit" [disabled]="form.invalid || saving">
        {{ saving ? 'Saving...' : 'Add Product' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width { width: 100%; margin-bottom: 8px; }`],
})
export class AddProductDialogComponent {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private dialogRef = inject(MatDialogRef<AddProductDialogComponent>);
  private snack = inject(MatSnackBar);

  saving = false;

  form = this.fb.group({
    name: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stock: [0, [Validators.required, Validators.min(0)]],
  });

  submit() {
    if (this.form.invalid) return;
    this.saving = true;
    this.productService.create(this.form.value as any).subscribe({
      next: () => { this.dialogRef.close(true); },
      error: () => { this.snack.open('Failed to create product', 'Close', { duration: 3000 }); this.saving = false; },
    });
  }
}
