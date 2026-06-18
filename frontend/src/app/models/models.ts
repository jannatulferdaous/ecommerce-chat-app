export interface ProductVariant {
  variantId: number;
  size: string;
  stock: number;
}

export interface Product {
  id: number;
  name: string;
  brand: string;
  description: string;
  price: number;
  imageUrl: string;
  category: string;
  variants: ProductVariant[];
}

export interface CartItem {
  cartItemId: number;
  productId: number;
  productName: string;
  brand: string;
  imageUrl: string;
  size: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  items: CartItem[];
  total: number;
}

export interface OrderItem {
  productName: string;
  size: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  orderDate: string;
  status: string;
  totalAmount: number;
  items: OrderItem[];
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  createdAt?: string;
}

export interface ChatResponse {
  reply: string;
  products?: Product[];
  cart?: Cart;
  order?: Order;
}

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  expiresAt: string;
}
