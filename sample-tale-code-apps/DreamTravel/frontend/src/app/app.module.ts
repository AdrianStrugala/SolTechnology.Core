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
import { RegisterComponent } from './register-page/register.component';
import { MyAccountComponent } from './my-account-page/my-account/my-account.component';
import { FlightOrderListComponent } from './my-account-page/flight-order-list/flight-order-list.component';
import { SuccessMessageComponent } from  './main-page/success-message/success-message.component';
import { AuthGuard } from './auth-guard';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { MapComponent } from './dream-trips/map/map.component';
import { CitiesPanelComponent } from './dream-trips/cities-panel/cities-panel.component';
import { ChangePasswordComponent } from './my-account-page/change-password/change-password.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

const routes: Routes = [
    { path: 'dream-flights', component: FlightEmailOrderComponent },
    { path: 'dream-trips', component: MapComponent },
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
        ChangePasswordComponent,
        RegisterComponent,
        MyAccountComponent,
        FlightOrderListComponent,
        SuccessMessageComponent,
        MapComponent,
        CitiesPanelComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        BrowserAnimationsModule,
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        MaterialModule,
        RouterModule.forRoot(routes),
        MatProgressSpinnerModule,
        FontAwesomeModule
    ],
    exports:[
        RouterModule
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
