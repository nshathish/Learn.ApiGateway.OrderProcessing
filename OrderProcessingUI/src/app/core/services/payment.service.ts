import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  PaymentMethod,
  PaymentResponse,
  PaymentSummary,
  ProcessPaymentRequest,
  RefundRequest,
  RefundResponse,
  Transaction,
} from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly base = `${environment.apiUrl}/api/payments`;

  constructor(private http: HttpClient) {}

  getMethods() {
    return this.http.get<PaymentMethod[]>(`${this.base}/methods`);
  }

  processPayment(req: ProcessPaymentRequest) {
    return this.http.post<PaymentResponse>(`${this.base}/process`, req);
  }

  getTransaction(transactionId: string) {
    return this.http.get<Transaction>(`${this.base}/transactions/${transactionId}`);
  }

  getUserTransactions(userId: number) {
    return this.http.get<Transaction[]>(`${this.base}/users/${userId}/transactions`);
  }

  refund(req: RefundRequest) {
    return this.http.post<RefundResponse>(`${this.base}/refund`, req);
  }

  getSummary(userId: number) {
    return this.http.get<PaymentSummary>(`${this.base}/users/${userId}/summary`);
  }
}
