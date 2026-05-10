import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CreateProductRequest, Product, UpdateStockRequest } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly base = `${environment.apiUrl}/api/products`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<Product[]>(this.base);
  }

  getById(id: number) {
    return this.http.get<Product>(`${this.base}/${id}`);
  }

  create(req: CreateProductRequest) {
    return this.http.post<Product>(this.base, req);
  }

  updateStock(id: number, req: UpdateStockRequest) {
    return this.http.put<Product>(`${this.base}/${id}/stock`, req);
  }
}
