import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';
import { ChatWidgetComponent } from './components/chat-widget/chat-widget.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, ChatWidgetComponent],
  template: `
    <app-navbar />
    <router-outlet />
    <app-chat-widget />
  `,
  styles: []
})
export class AppComponent {}
