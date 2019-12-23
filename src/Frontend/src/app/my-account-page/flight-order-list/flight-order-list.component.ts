import { Component, ChangeDetectionStrategy } from '@angular/core';
import { UserService } from '../../user.service';
import { Observable } from 'rxjs';
import { FlightEmailSubscriptionService, IFlightEmailSubscription } from '../../flight-email-subscription.service';

@Component({
  selector: 'flight-order-list',
  templateUrl: './flight-order-list.component.html',
  styleUrls: ['./flight-order-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FlightOrderListComponent {

  public flightEmailOrders$: Observable<IFlightEmailSubscription[]> = this.flightEmailSubscriptionService.GetByUserId(this.userService.user.id);

  constructor(
    private userService: UserService,
    private flightEmailSubscriptionService: FlightEmailSubscriptionService) { }

  cancel(id: number){
    this.flightEmailSubscriptionService.Delete(id);
  }
}
