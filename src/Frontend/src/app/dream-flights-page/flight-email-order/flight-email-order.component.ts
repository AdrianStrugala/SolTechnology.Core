import { Component, OnInit, Inject } from '@angular/core';
import { FormControl, FormGroup, Validators, FormBuilder, ValidatorFn, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MAT_MOMENT_DATE_FORMATS, MomentDateAdapter, MAT_MOMENT_DATE_ADAPTER_OPTIONS } from '@angular/material-moment-adapter';
import { DateAdapter, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';
import { UserService } from '../../user.service';
import { IAirport, AirportsService } from './airports.service';
import { Observable, of } from 'rxjs';
import { map, startWith, filter, catchError } from 'rxjs/operators';


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



    ageRangeValidator(filteredOptions: Observable<string[]>) { 
        return (ctrl: AbstractControl): Promise<ValidationErrors | null> | Observable<ValidationErrors | null> => {

        console.log(filteredOptions);

        return filteredOptions
            .pipe(
                map((filtered: String[]) => 
                     (filtered.length == 0) ? { autocomplete: true } : null),
                catchError(() => null)
            );

        // if (pickedOrNot.length > 0) {
        //     // everything's fine. return no error. therefore it's null.
        //     return of(null);

        // } else {
        //     //there's no matching selectboxvalue selected. so return match error.
        //     return {autocomplete: true};
        // }

    };
    }
    

    // valueSelected(fromFilter: Observable<string[]>): AsyncValidatorFn {

    //     // console.log(value);


    //     // if (value.untouched) {
    //     //     return null;
    //     // }
    //     return (control: AbstractControl): Promise<ValidationErrors | null> | Observable<ValidationErrors | null> => {
    //         return fromFilter
    //             .pipe(
    //                 map((filtered: String[]) => {
    //                     return (filtered.filter(option => option.toLowerCase().includes(control.value)).length == 0) ? { autocomplete: true } : null;

    //                     // if (pickedOrNot.length > 0) {
    //                     //     // everything's fine. return no error. therefore it's null.
    //                     //     return of(null);

    //                     // } else {
    //                     //     //there's no matching selectboxvalue selected. so return match error.
    //                     //     return {autocomplete: true};
    //                     // }
    //                 })
    //             )
    //     }
    // }

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
        from: new FormControl(null,
            {
                validators: [Validators.required],
                asyncValidators: [this.ageRangeValidator(this.filteredOptions)]
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

    // private _countInArray(value: string, array: string[]): Observable<number> {
    //     const filterValue = value.toLowerCase();
    //     return this.autocomplete.filter(option => option.toLowerCase().includes(filterValue));
    // }

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

export class FormCustomValidators {
    static valueSelected(myArray: any[]): ValidatorFn {

        console.log(myArray);

        return (c: AbstractControl): { [key: string]: boolean } | null => {

            if (c.untouched) {
                return null;
            }

            let pickedOrNot = myArray.filter(alias => alias.name === c.value);

            if (pickedOrNot.length > 0) {
                // everything's fine. return no error. therefore it's null.
                return null;

            } else {
                //there's no matching selectboxvalue selected. so return match error.
                return { 'match': true };
            }
        }
    }
}