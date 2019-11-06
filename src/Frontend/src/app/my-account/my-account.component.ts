import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';

@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent implements OnInit {

  constructor( public userService: UserService) { }

  ngOnInit() {
  }

  logout(){
    this.userService.logout();

    localStorage.removeItem("user");
  }

}
