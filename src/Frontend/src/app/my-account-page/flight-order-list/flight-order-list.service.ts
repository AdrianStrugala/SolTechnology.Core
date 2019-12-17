import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class FlightOrderListService {

  constructor(private http: HttpClient) { }

  url = "https://dreamtravelsapi-demo.azurewebsites.net/api/GetFlightEmailOrders/";

  GetFlightEmailOrdersForUser(userId: number): Observable<IFlightEmailOrder[]> {

    return this.http.get<IFlightEmailOrder[]>(
      this.url + userId,
      {
        observe: "body"
      })
      .pipe(
        catchError(err => {
          console.error(err);
          return of([] as IFlightEmailOrder[])
        })
      );
  }
}

export interface IFlightEmailOrder {
  userId: number;
  from: string;
  to: string;
  departureDate: Date;
  arrivalDate: Date;
  minDaysOfStay: number;
  maxDaysOfStay: number;
}