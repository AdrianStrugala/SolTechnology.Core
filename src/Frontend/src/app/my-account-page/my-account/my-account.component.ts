import { Component } from '@angular/core';
import { UserService } from '../../user.service';
import { Router } from '@angular/router';


@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent {

  constructor(public userService: UserService, private router: Router) { }

  logout() {
    this.userService.logout();

    this.router.navigate([""]);
  }
}
