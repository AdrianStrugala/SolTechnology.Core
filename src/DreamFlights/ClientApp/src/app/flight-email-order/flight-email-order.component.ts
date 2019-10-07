import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators, FormBuilder } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'flight-email-order',
  templateUrl: './flight-email-order.component.html',
  styleUrls: ['./flight-email-order.component.scss'],
})

export class FlightEmailOrderComponent implements OnInit {

  constructor(private http: HttpClient) { }

//  url = "https://dreamtravel-demo.azurewebsites.net/api/OrderFlightEmail";
  url = "http://localhost:5754/api/OrderFlightEmail";

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

