import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AddItemRequest, Cart } from '../models/cart.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly base = `${environment.apiUrl}/api/cart`;

  constructor(private http: HttpClient) {}

  getCart(userId: number) {
    return this.http.get<Cart>(`${this.base}/${userId}`);
  }

  addItem(userId: number, req: AddItemRequest) {
    return this.http.post<Cart>(`${this.base}/${userId}/items`, req);
  }

  removeItem(userId: number, productId: number) {
    return this.http.delete<Cart>(`${this.base}/${userId}/items/${productId}`);
  }
}
