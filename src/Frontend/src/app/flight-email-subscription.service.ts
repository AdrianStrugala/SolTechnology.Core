import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError } from "rxjs/operators";

@Injectable({
  providedIn: "root"
})
export class FlightEmailSubscriptionService {
  constructor(private http: HttpClient) {}

  url = "https://dreamtravelsapi-demo.azurewebsites.net/api/FlightEmailSubscription/";

  Delete(id: number): Observable<any> {
    return this.http
      .delete(this.url + id)
  }

  Insert(value: any): Observable<any> {
    return this.http
    .post(this.url, value, {
      observe: "body"
    })
  }

  GetByUserId(userId: number): Observable<IFlightEmailSubscription[]> {
    return this.http.get<IFlightEmailSubscription[]>(
      this.url + userId,
      {
        observe: "body"
      })
      .pipe(
        catchError(err => {
          console.error(err);
          return of([] as IFlightEmailSubscription[])
        })
      );
  }
}


export interface IFlightEmailSubscription {
  id: number;
  userId: number;
  from: string;
  to: string;
  departureDate: Date;
  arrivalDate: Date;
  minDaysOfStay: number;
  maxDaysOfStay: number;
}
