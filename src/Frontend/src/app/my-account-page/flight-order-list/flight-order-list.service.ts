import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FlightOrderListService{

  constructor(private http: HttpClient) {}

  url = "https://dreamtravelsapi.azurewebsites.net/api/GetFlightEmailOrders/";

  GetFlightEmailOrdersForUser(userId : number): Observable<any> {

  return this.http.get(
      this.url + userId,
      {
          observe: "body"
      });
  } 
}

export interface IFlightEmailOrder { 
  userId: number;
  from: string;
  to: string;
  departureDate: Date;
  arrivalDate: Date;
  minDaysOfStay : number;
  maxDaysOfStay : number;
}