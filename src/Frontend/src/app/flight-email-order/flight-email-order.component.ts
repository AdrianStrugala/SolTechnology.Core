import { Component, OnInit, Inject } from '@angular/core';
import { FormControl, FormGroup, Validators, FormBuilder } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MAT_MOMENT_DATE_FORMATS, MomentDateAdapter, MAT_MOMENT_DATE_ADAPTER_OPTIONS } from '@angular/material-moment-adapter';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';


@Component({
    selector: 'flight-email-order',
    templateUrl: './flight-email-order.component.html',
    styleUrls: ['./flight-email-order.component.scss'],

    providers: [
        { provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS, useValue: { useUtc: true } },
        // The locale would typically be provided on the root module of your application. We do it at
        // the component level here, due to limitations of our example generation script.
        { provide: MAT_DATE_LOCALE, useValue: 'en-GB' },

        // `MomentDateAdapter` and `MAT_MOMENT_DATE_FORMATS` can be automatically provided by importing
        // `MatMomentDateModule` in your applications root module. We provide it at the component level
        // here, due to limitations of our example generation script.
        { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE, MAT_MOMENT_DATE_ADAPTER_OPTIONS] },
        { provide: MAT_DATE_FORMATS, useValue: MAT_MOMENT_DATE_FORMATS },
    ]
})

export class FlightEmailOrderComponent implements OnInit {

    constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

    url = "https://dreamtravelsapi-demo.azurewebsites.net/api/OrderFlightEmail";

    orderForm = new FormGroup({
        from: new FormControl(),
        to: new FormControl(),
        departureDate: new FormControl(),
        arrivalDate: new FormControl(),
        minDaysOfStay: new FormControl(),
        maxDaysOfStay: new FormControl(),
    });

    onSubmit(): void {
        console.log(this.orderForm.value);

        this.http.post(
            this.url,
            this.orderForm.value,
            {
                observe: "body",
                withCredentials: true
            })
            .subscribe();
    }

    ngOnInit() {
    }
}

