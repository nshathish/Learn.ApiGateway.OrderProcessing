import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CheckoutPageResponse, CreateOrderRequest } from '../models/checkout.model';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly base = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  getCheckoutPage(userId: number) {
    return this.http.get<CheckoutPageResponse>(`${this.base}/checkout-page/${userId}`);
  }

  createOrder(req: CreateOrderRequest) {
    return this.http.post<unknown>(`${this.base}/orders`, req);
  }
}
