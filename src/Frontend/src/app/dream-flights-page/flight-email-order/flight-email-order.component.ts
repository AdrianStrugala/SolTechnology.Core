import { Component, OnInit, Inject } from '@angular/core';
import { FormControl, FormGroup, Validators, FormBuilder, ValidatorFn } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MAT_MOMENT_DATE_FORMATS, MomentDateAdapter, MAT_MOMENT_DATE_ADAPTER_OPTIONS } from '@angular/material-moment-adapter';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';
import { UserService } from '../../user.service';
import { IAirport, AirportsService } from './airports.service';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';


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

    constructor(private http: HttpClient, private userService: UserService, private ariportService: AirportsService) { }

    url = "https://dreamtravelsapi-demo.azurewebsites.net/api/OrderFlightEmail";


    airports: IAirport[];
    autocomplete: string[];
    filteredOptions: Observable<string[]>;


    minDaysValidator: ValidatorFn = (orderForm: FormGroup) => {
        let minDays = orderForm.get('minDaysOfStay').value;
        let arrivalDate = orderForm.get('arrivalDate').value;
        let departureDate = orderForm.get('departureDate').value;

        let differenceInTime = arrivalDate - departureDate;
        var differenceInDays = differenceInTime / (1000 * 3600 * 24);

        if (minDays > differenceInDays) {
            return { minDays }
        }
        return null;
    }

    arrivalDateValidator: ValidatorFn = (orderForm: FormGroup) => {
        let arrivalDate = orderForm.get('arrivalDate').value;
        let departureDate = orderForm.get('departureDate').value;

        if (departureDate > arrivalDate) {
            return { arrivalDate }
        }
        return null;
    }

    maxDaysValidator: ValidatorFn = (orderForm: FormGroup) => {
        let minDays = orderForm.get('minDaysOfStay').value;
        let maxDays = orderForm.get('maxDaysOfStay').value;

        if (minDays > maxDays) {
            return { maxDays }
        }
        return null;
    }

    orderForm = new FormGroup({
        from: new FormControl('', {
            validators: [Validators.required]
        }),
        to: new FormControl('', {
            validators: [Validators.required]
        }),
        departureDate: new FormControl(new Date(), {
            validators: [Validators.required]
        }),
        arrivalDate: new FormControl('', {
            validators: [Validators.required]
        }),
        minDaysOfStay: new FormControl('', {
            validators: [Validators.required, Validators.min(1)]
            , updateOn: "blur"
        }),
        maxDaysOfStay: new FormControl('', {
            validators: [Validators.required, Validators.min(1)]
        }),
        userId: new FormControl
    }, [this.minDaysValidator, this.arrivalDateValidator, this.maxDaysValidator]);

    minDepartureDate = new Date();
    minArrivalDate = this.orderForm.value.departureDate;


    ngOnInit() {
        this.ariportService.getAirports().subscribe(
            (data: IAirport[]) => {
                this.airports = data;
                this.autocomplete = this.airports.map(a => a.name);
            
                //from autocomplete
                this.filteredOptions = this.orderForm.get('from').valueChanges
                .pipe(
                    startWith(''),
                    map(value => this._filter(value))
                );
            }
        )
    }

    private _filter(value: string): string[] {
        const filterValue = value.toLowerCase();
        return this.autocomplete.filter(option => option.toLowerCase().includes(filterValue));
    }

    onSubmit(): void {

        this.orderForm.value.userId = this.userService.user.id

        this.http.post(
            this.url,
            this.orderForm.value,
            {
                observe: "body"
            })
            .subscribe();
    }
}

