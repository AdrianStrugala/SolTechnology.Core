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
import { Router } from "@angular/router";

@Component({
  selector: "flight-order-list",
  templateUrl: "./flight-order-list.component.html",
  styleUrls: ["./flight-order-list.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FlightOrderListComponent {
  public flightEmailOrders$: Observable<IFlightEmailSubscription[]> = this.flightEmailSubscriptionService.GetByUserId(this.userService.user.id);

  private dayChangedEvents: DayChangedEvent[] = [];

  constructor(
    private userService: UserService,
    private flightEmailSubscriptionService: FlightEmailSubscriptionService)
     {}

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
        this.userService.user.id
      ).subscribe();
  }
}


