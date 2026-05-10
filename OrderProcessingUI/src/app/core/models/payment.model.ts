export interface PaymentMethod {
  id: number;
  name: string;
  code: string;
  icon: string;
  processingFee: number;
  supportedCurrencies: string[];
}

export interface ProcessPaymentRequest {
  orderId: string;
  userId: number;
  amount: number;
  currency: string;
  paymentMethodId: number;
  cardNumber?: string;
  cardExpiry?: string;
  cardCvv?: string;
  cardLast4?: string;
  billingAddress?: string;
}

export interface PaymentResponse {
  success: boolean;
  transactionId: string;
  amount: number;
  currency: string;
  status: string;
  message: string;
  processingFee: number;
  totalAmount: number;
}

export interface Transaction {
  id: number;
  orderId: string;
  userId: number;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: string;
  transactionId: string;
  createdAt: string;
  errorMessage?: string;
}

export interface RefundRequest {
  transactionId: string;
  amount: number;
  reason: string;
}

export interface RefundResponse {
  success: boolean;
  refundId: string;
  amount: number;
  status: string;
  message: string;
}

export interface PaymentSummary {
  userId: number;
  totalSpent: number;
  totalRefunded: number;
  netSpent: number;
  transactionCount: number;
  refundCount: number;
  lastTransactionDate: string;
}
