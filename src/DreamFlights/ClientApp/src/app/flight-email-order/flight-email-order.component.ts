import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators, FormBuilder } from '@angular/forms';

@Component({
  selector: 'flight-email-order',
  templateUrl: './flight-email-order.component.html',
  styleUrls: ['./flight-email-order.component.scss'],
})

export class FlightEmailOrderComponent implements OnInit {

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
  }

  ngOnInit() {
  }
}

