import { Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { LoginComponent } from '../login/login.component';

@Component({
    selector: 'navbar',
    templateUrl: './navbar.component.html',
    styleUrls: ['./navbar.component.scss']
})

export class NavbarComponent{

    animal: string;
    name: string;

    constructor(public dialog: MatDialog) { }

    openDialog(): void {
        let dialogRef = this.dialog.open(LoginComponent, {
            height: '400px',
            width: '600px',
            data: {name: this.name, animal: this.animal}
          });
    
        dialogRef.afterClosed().subscribe(result => {
          console.log('The dialog was closed');
          this.animal = result;
        });
      }
}

