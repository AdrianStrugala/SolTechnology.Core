import { Component, AfterViewInit } from '@angular/core';
import { UserService } from '../../user.service';
import { FlightOrderListService, IFlightEmailOrder } from './flight-order-list.service'
import { Observable } from 'rxjs';

@Component({
  selector: 'flight-order-list',
  templateUrl: './flight-order-list.component.html',
  styleUrls: ['./flight-order-list.component.scss']
})
export class FlightOrderListComponent implements AfterViewInit {

  public flightEmailOrders$: Observable<IFlightEmailOrder[]>;

  constructor(private userService: UserService, private flightEmailOrderService: FlightOrderListService) { }

  ngAfterViewInit() {
    this.flightEmailOrders$ = this.flightEmailOrderService.GetFlightEmailOrdersForUser(this.userService.user.id);
  }
}
