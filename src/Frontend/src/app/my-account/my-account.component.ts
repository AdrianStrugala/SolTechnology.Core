import { Component, OnInit } from '@angular/core';
import { UserService } from '../user.service';
import { FlightEmailOrderService, IFlightEmailOrder } from '../flight-email-order.service'


@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent implements OnInit {

  public flightEmailOrders: IFlightEmailOrder[];


  constructor(public userService: UserService, private flightEmailOrderService: FlightEmailOrderService) { }

  ngOnInit() {
    this.flightEmailOrderService.GetFlightEmailOrdersForUser(this.userService.user.Id)
      .subscribe(
        (data: IFlightEmailOrder[]) => {
          this.flightEmailOrders = data;

          console.log(data.toString)
        },
        (error) => {
          //ignore
        });
  }

  logout() {
    this.userService.logout();

    localStorage.removeItem("user");
  }

}
