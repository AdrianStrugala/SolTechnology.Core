import { Component, Inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { LoginComponent } from '../login/login.component';

@Component({
    selector: 'navbar',
    templateUrl: './navbar.component.html',
    styleUrls: ['./navbar.component.scss']
})

export class NavbarComponent{

    constructor(public dialog: MatDialog) { }

    openDialog(): void {
        let dialogRef = this.dialog.open(LoginComponent, {
            height: '500px',
            width: '500px'
          });
      }
    }
  

