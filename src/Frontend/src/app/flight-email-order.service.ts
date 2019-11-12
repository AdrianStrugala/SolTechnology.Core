import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FlightEmailOrderService{

  
  constructor(private http: HttpClient) { 
    
  }

  url = "https://dreamtravelsapi-demo.azurewebsites.net/GetFlightEmailOrders/";

  GetFlightEmailOrdersForUser(userId : number): Observable<any> {

  return this.http.get(
      this.url + "userId",
      {
          observe: "body"
      });
  } 
}

export interface IFlightEmailOrder { 
  UserId: number;
  From: string;
  To: string;
  DepartureDate: Date;
  ArrivalDate: Date;
  MinDaysOfStay : number;
  MaxDaysOfStay : number;
}