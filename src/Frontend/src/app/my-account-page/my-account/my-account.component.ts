import { Component } from '@angular/core';
import { UserService } from '../../user.service';
import { Router } from '@angular/router';


@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent {

  constructor(private userService: UserService, private router: Router) { }

  logout() {
    this.userService.logout();

    localStorage.removeItem("user");

    this.router.navigate([""]);
  }
}