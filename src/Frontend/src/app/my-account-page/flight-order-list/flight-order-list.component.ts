import { Component, ChangeDetectionStrategy } from '@angular/core';
import { UserService } from '../../user.service';
import { FlightOrderListService, IFlightEmailOrder } from './flight-order-list.service'
import { Observable } from 'rxjs';

@Component({
  selector: 'flight-order-list',
  templateUrl: './flight-order-list.component.html',
  styleUrls: ['./flight-order-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FlightOrderListComponent {

  public flightEmailOrders$: Observable<IFlightEmailOrder[]> = this.flightEmailOrderService.GetFlightEmailOrdersForUser(this.userService.user.id);

  constructor(private userService: UserService, private flightEmailOrderService: FlightOrderListService) { }

}
