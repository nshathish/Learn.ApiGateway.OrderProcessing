import { Cart } from './cart.model';
import { PaymentMethod } from './payment.model';
import { Product } from './product.model';
import { User } from './user.model';

export interface CheckoutPageResponse {
  products: Product[];
  cart: Cart;
  user: User;
  paymentMethods: PaymentMethod[];
}

export interface CreateOrderRequest {
  userId: number;
  productIds: number[];
}
