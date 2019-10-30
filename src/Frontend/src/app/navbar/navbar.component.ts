import { Component } from '@angular/core';
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

        if(this.dialog.openDialogs.length == 0){
            this.dialog.open(LoginComponent, {
                height: '35rem',
                width: '30rem'
              });
        }
        else{
            this.dialog.closeAll();
        }
    }
}
  

