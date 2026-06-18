import { Routes } from '@angular/router';
import { ProductListComponent } from './components/product-list/product-list.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { CartComponent } from './components/cart/cart.component';
import { OrdersComponent } from './components/checkout/orders.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: ProductListComponent, title: 'Shop' },
  { path: 'login', component: LoginComponent, title: 'Log in' },
  { path: 'register', component: RegisterComponent, title: 'Create account' },
  { path: 'cart', component: CartComponent, canActivate: [authGuard], title: 'Your cart' },
  { path: 'orders', component: OrdersComponent, canActivate: [authGuard], title: 'Your orders' },
  { path: '**', redirectTo: '' }
];
