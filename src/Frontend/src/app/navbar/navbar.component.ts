import { Component, Inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { LoginComponent } from '../login/login.component';
import { UserService } from '../user.service';

@Component({
    selector: 'navbar',
    templateUrl: './navbar.component.html',
    styleUrls: ['./navbar.component.scss']
})

export class NavbarComponent{


    constructor(public dialog: MatDialog, private userService: UserService) { }

    openDialog(): void {
        let dialogRef = this.dialog.open(LoginComponent, {
            height: '500px',
            width: '500px',
            data : {Email: this.userService.Email, Password: this.userService.Password}
          });
    

        dialogRef.afterClosed().subscribe(result => {
          this.userService.Email = result.email;
          this.userService.Password = result.password;
          console.log(this.userService.Email)

          var xd  = this.userService.login();
          console.log(xd)
        });
      }
}

