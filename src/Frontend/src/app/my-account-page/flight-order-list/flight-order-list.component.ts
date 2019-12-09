import { Component, AfterViewInit } from '@angular/core';
import { UserService } from '../../user.service';
import { FlightOrderListService, IFlightEmailOrder } from './flight-order-list.service'  
  
  @Component({
    selector: 'flight-order-list',
    templateUrl: './flight-order-list.component.html',
    styleUrls: ['./flight-order-list.component.scss']
  })
  export class FlightOrderListComponent implements AfterViewInit {
  
    public flightEmailOrders: IFlightEmailOrder[];
  
  
    constructor(private userService: UserService, private flightEmailOrderService: FlightOrderListService) { }
  
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
}
