export interface Product {
  id: number;
  name: string;
  price: number;
  stock: number;
}

export interface CreateProductRequest {
  name: string;
  price: number;
  stock: number;
}

export interface UpdateStockRequest {
  quantity: number;
}
