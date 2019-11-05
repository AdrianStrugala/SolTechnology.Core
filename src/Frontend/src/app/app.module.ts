import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { Routes, RouterModule } from '@angular/router';
import { FlightEmailOrderComponent } from './flight-email-order/flight-email-order.component'
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { NavbarComponent } from './navbar/navbar.component';
import { MaterialModule } from './external-libraries/material-module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';

const routes: Routes = [
    { path: 'register', component: RegisterComponent },
    { path: '', component: HomeComponent, pathMatch: 'full' }
  ];

@NgModule({
    declarations: [
        AppComponent,
        HomeComponent,
        NavbarComponent,
        FlightEmailOrderComponent,
        LoginComponent,
        RegisterComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        BrowserAnimationsModule,
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        MaterialModule,
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
