import { Component } from '@angular/core';
import { UserService } from '../../user.service';
import { MatDialog } from "@angular/material/dialog";
import { ChangePasswordComponent } from '../change-password/change-password.component';


@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent {

  constructor(public dialog: MatDialog, public userService: UserService) { }

  changePassword(): void {
    if (this.dialog.openDialogs.length == 0) {
      this.dialog.open(ChangePasswordComponent, {
        height: "35rem",
        width: "30rem"
      });
    } else {
      this.dialog.closeAll();
    }
  }
}
