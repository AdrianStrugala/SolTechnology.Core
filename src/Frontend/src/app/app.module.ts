import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { Routes, RouterModule } from '@angular/router';
import { FlightEmailOrderComponent } from './dream-flights-page/flight-email-order/flight-email-order.component'
import { AppComponent } from './app.component';
import { HomeComponent } from './main-page/home/home.component';
import { NavbarComponent } from './main-page/navbar/navbar.component';
import { MaterialModule } from './external-libraries/material-module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { LoginComponent } from './main-page/login/login.component';
import { RegisterComponent } from './register/register.component';
import { MyAccountComponent } from './my-account-page/my-account/my-account.component';
import { FlightOrderListComponent } from './my-account-page/flight-order-list/flight-order-list.component';
import { SuccessMessageComponent } from  './main-page/success-message/success-message.component';
import { AuthGuard } from './auth-guard';
import { AngularFontAwesomeModule } from 'angular-font-awesome';

const routes: Routes = [
    { path: 'flight-email-subscription', component: FlightEmailOrderComponent },
    { path: 'register', component: RegisterComponent },
    { path: 'my-account', component: MyAccountComponent, canActivate: [AuthGuard] },
    { path: '', component: HomeComponent, pathMatch: 'full' }
  ];

@NgModule({
    declarations: [
        AppComponent,
        HomeComponent,
        NavbarComponent,
        FlightEmailOrderComponent,
        LoginComponent,
        RegisterComponent,
        MyAccountComponent,
        FlightOrderListComponent,
        SuccessMessageComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        BrowserAnimationsModule,
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        MaterialModule,
        AngularFontAwesomeModule,
        RouterModule.forRoot(routes)
    ],
    exports:[
        RouterModule
    ],
    entryComponents: [
        LoginComponent
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
