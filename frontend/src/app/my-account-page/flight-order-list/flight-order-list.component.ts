import { Component, ChangeDetectionStrategy, OnInit } from "@angular/core";
import { UserService } from "../../user.service";
import { Observable } from "rxjs";
import { HostListener } from "@angular/core";
import {
  FlightEmailSubscriptionService,
  IFlightEmailSubscription,
  DayChangedEvent
} from "../../flight-email-subscription.service";
import { Route } from "@angular/compiler/src/core";
import { Router, NavigationStart, NavigationEnd, NavigationError } from "@angular/router";

@Component({
  selector: "flight-order-list",
  templateUrl: "./flight-order-list.component.html",
  styleUrls: ["./flight-order-list.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FlightOrderListComponent {
  public flightEmailOrders$: Observable<IFlightEmailSubscription[]> = this.flightEmailSubscriptionService.GetByUserId(this.userService.user.userId);

  private dayChangedEvents: DayChangedEvent[] = [];

  constructor(
    private userService: UserService,
    private flightEmailSubscriptionService: FlightEmailSubscriptionService,
    private router: Router)
     {
      this.router.events.subscribe((event) => {
        if (event instanceof NavigationStart) {
            // Show loading indicator
        }

        if (event instanceof NavigationEnd) {
          console.log("xd");
          this.flightEmailSubscriptionService.UpdateSubscriptionsForUser(
            this.dayChangedEvents,
            this.userService.user.userId
          ).subscribe();
        }

        if (event instanceof NavigationError) {
            // Hide loading indicator

            // Present error to user
            console.log(event.error);
        }
    });
     }

  cancel(id: number) {
    this.flightEmailSubscriptionService.Delete(id).subscribe();
  }

onDayChange(id, day, value){

  var event = new DayChangedEvent;
  event.value = value;
  event.day = day;
  event.subscriptionId = id;

  this.dayChangedEvents.push(event)
}

  @HostListener('window:beforeunload', ['$event'])
  updateSubscriptions($event: any) {
    console.log("xd");
      this.flightEmailSubscriptionService.UpdateSubscriptionsForUser(
        this.dayChangedEvents,
        this.userService.user.userId
      ).subscribe();
  }
}


