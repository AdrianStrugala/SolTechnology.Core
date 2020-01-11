import { Component } from "@angular/core";
import {
  FormControl,
  FormGroup,
  Validators,
  ValidatorFn,
  AsyncValidatorFn
} from "@angular/forms";
import {
  MAT_MOMENT_DATE_FORMATS,
  MomentDateAdapter,
  MAT_MOMENT_DATE_ADAPTER_OPTIONS
} from "@angular/material-moment-adapter";
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE
} from "@angular/material/core";
import { UserService } from "../../user.service";
import { IAirport, AirportsService } from "../airports.service";
import { Observable, combineLatest, of } from "rxjs";
import { map } from "rxjs/operators";
import { SuccessMessageService } from "../../main-page/success-message/success-message.service";
import { Router } from "@angular/router";
import { handleError } from "../../shared/error";
import { FlightEmailSubscriptionService } from "../../flight-email-subscription.service";

@Component({
  selector: "flight-email-order",
  templateUrl: "./flight-email-order.component.html",
  styleUrls: ["./flight-email-order.component.scss"],

  providers: [
    { provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS, useValue: { useUtc: true } },
    // The locale would typically be provided on the root module of your application. We do it at
    // the component level here, due to limitations of our example generation script.
    { provide: MAT_DATE_LOCALE, useValue: "en-GB" },

    // `MomentDateAdapter` and `MAT_MOMENT_DATE_FORMATS` can be automatically provided by importing
    // `MatMomentDateModule` in your applications root module. We provide it at the component level
    // here, due to limitations of our example generation script.
    {
      provide: DateAdapter,
      useClass: MomentDateAdapter,
      deps: [MAT_DATE_LOCALE, MAT_MOMENT_DATE_ADAPTER_OPTIONS]
    },
    { provide: MAT_DATE_FORMATS, useValue: MAT_MOMENT_DATE_FORMATS }
  ]
})
export class FlightEmailOrderComponent {
  airportsSubscription: any;
  constructor(
    private userService: UserService,
    private ariportService: AirportsService,
    private successMessageService: SuccessMessageService,
    private router: Router,
    private flightEmailSubscriptionService: FlightEmailSubscriptionService
  ) {}

  orderInProgress = false;
  error: string;
  airports: IAirport[];
  autocomplete: string[];

  minDaysValidator: ValidatorFn = (orderForm: FormGroup) => {
    let minDays = orderForm.get("minDaysOfStay").value;
    let arrivalDate = orderForm.get("arrivalDate").value;
    let departureDate = orderForm.get("departureDate").value;

    let differenceInTime = arrivalDate - departureDate;
    var differenceInDays = differenceInTime / (1000 * 3600 * 24);

    if (minDays > differenceInDays) {
      return { minDays };
    }
    return null;
  };

  arrivalDateValidator: ValidatorFn = (orderForm: FormGroup) => {
    let arrivalDate = orderForm.get("arrivalDate").value;
    let departureDate = orderForm.get("departureDate").value;

    if (departureDate > arrivalDate) {
      return { arrivalDate };
    }
    return null;
  };

  maxDaysValidator: ValidatorFn = (orderForm: FormGroup) => {
    let minDays = orderForm.get("minDaysOfStay").value;
    let maxDays = orderForm.get("maxDaysOfStay").value;

    if (minDays > maxDays) {
      return { maxDays };
    }
    return null;
  };

  loggedInValidator: ValidatorFn = () => {
    if (!this.userService.isLoggedIn()) {
      return { login: true };
    }
    return null;
  };
  autocompleteValidatorFrom: AsyncValidatorFn = (control: FormControl) => {
    this.orderForm.controls["from"].setErrors(null);

    return this.filteredFrom$.pipe(
      map(res => {
        let airport = res.filter(
          a => a.name.toLowerCase() === control.value.toLowerCase()
        );
        if (airport.length == 0) {
          this.orderForm.controls["from"].setErrors({ autocomplete: true });
        }
      })
    );
    return of(null);
  };

  autocompleteValidatorTo: AsyncValidatorFn = (control: FormControl) => {
    this.orderForm.controls["to"].setErrors(null);

    return this.filteredTo$.pipe(
      map(res => {
        let airport = res.filter(
          a => a.name.toLowerCase() === control.value.toLowerCase()
        );
        if (airport.length == 0) {
          this.orderForm.controls["to"].setErrors({ autocomplete: true });
        }
      })
    );
    return of(null);
  };

  emailForm = new FormGroup(
    {
      monday: new FormControl(true),
      tuesday: new FormControl(true),
      wendesday: new FormControl(true),
      thursday: new FormControl(true),
      friday: new FormControl(true),
      saturday: new FormControl(true),
      sunday: new FormControl(true)
    }
  );

  orderForm = new FormGroup(
    {
      from: new FormControl("", {
        validators: [Validators.required],
        asyncValidators: [this.autocompleteValidatorFrom]
      }),
      to: new FormControl("", {
        validators: [Validators.required],
        asyncValidators: [this.autocompleteValidatorTo]
      }),
      departureDate: new FormControl(new Date(), {
        validators: [Validators.required]
      }),
      arrivalDate: new FormControl("", {
        validators: [Validators.required]
      }),
      minDaysOfStay: new FormControl("", {
        validators: [Validators.required, Validators.min(1)],
        updateOn: "blur"
      }),
      maxDaysOfStay: new FormControl("", {
        validators: [Validators.required, Validators.min(1)]
      }),
      userId: new FormControl()
    },
    [
      this.minDaysValidator,
      this.arrivalDateValidator,
      this.maxDaysValidator,
      this.loggedInValidator
    ]
  );

  filteredFrom$: Observable<IAirport[]> = combineLatest([
    this.orderForm.get("from").valueChanges,
    this.ariportService.airports$
  ]).pipe(
    map(([from, airports]) =>
      airports.filter(a => a.name.toLowerCase().includes(from.toLowerCase()))
    )
  );

  filteredTo$: Observable<IAirport[]> = combineLatest([
    this.orderForm.get("to").valueChanges,
    this.ariportService.airports$
  ]).pipe(
    map(([from, airports]) =>
      airports.filter(a => a.name.toLowerCase().includes(from.toLowerCase()))
    )
  );

  minDepartureDate = new Date();
  minArrivalDate = this.orderForm.value.departureDate;


onClick(day: string){
  this.emailForm.controls[day].setValue(!this.emailForm.controls[day].value)
}


  onSubmit(): void {
    this.orderInProgress = true;
    this.error = null;
    this.orderForm.value.userId = this.userService.user.id;

    this.flightEmailSubscriptionService.Insert(this.orderForm.value, this.emailForm.value).subscribe(
      () => {
        this.successMessageService.set("Order successfully placed!");
        this.router.navigate([""]);
      },
      error => {
        this.error = handleError(error);
      }
    );

    this.orderInProgress = false;
  }
}
