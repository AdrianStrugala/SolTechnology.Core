import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { WorkerComponent } from './worker/worker.component';
import { GraphComponent } from './graph/graph.component';


@
  NgModule({
    declarations: [
      AppComponent,
      NavMenuComponent,
      HomeComponent,
      WorkerComponent,
      GraphComponent
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
      WorkerComponent
    ],
    bootstrap: [AppComponent]
  })
export class AppModule { }
