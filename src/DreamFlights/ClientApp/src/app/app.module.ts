import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { FlightEmailOrderComponent } from './flight-email-order/flight-email-order.component'
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';


@
  NgModule({
    declarations: [
      AppComponent,
      HomeComponent,
      FlightEmailOrderComponent
    ],
    imports: [
      BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
      HttpClientModule,
      FormsModule,
      RouterModule.forRoot([
        { path: '', component: HomeComponent, pathMatch: 'full' }
      ])
    ],
    entryComponents: [
    ],
    bootstrap: [AppComponent]
  })
export class AppModule { }
