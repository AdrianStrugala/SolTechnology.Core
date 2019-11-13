import { Component, OnInit, AfterViewInit } from '@angular/core';
import { UserService } from '../user.service';
import { FlightEmailOrderService, IFlightEmailOrder } from '../flight-email-order.service'


@Component({
  selector: 'app-my-account',
  templateUrl: './my-account.component.html',
  styleUrls: ['./my-account.component.scss']
})
export class MyAccountComponent implements AfterViewInit {

  public flightEmailOrders: IFlightEmailOrder[];


  constructor(private userService: UserService, private flightEmailOrderService: FlightEmailOrderService) { }

  ngAfterViewInit() {

    console.log(this.userService.user)

    this.flightEmailOrderService.GetFlightEmailOrdersForUser(this.userService.user.id)
      .subscribe(
        (data: IFlightEmailOrder[]) => {
          this.flightEmailOrders = data;

          console.log(data)
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
