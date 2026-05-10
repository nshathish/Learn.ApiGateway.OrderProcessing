export interface User {
  id: number;
  name: string;
  email: string;
  address: string;
  phone: string;
  createdAt: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  address: string;
  phone: string;
}

export interface UpdateUserRequest {
  name?: string;
  address?: string;
  phone?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  id: number;
  name: string;
  email: string;
  token: string;
}

export interface AddressHistory {
  id: number;
  address: string;
  createdAt: string;
}
