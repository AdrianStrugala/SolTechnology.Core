import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError } from "rxjs/operators";
import { Configuration } from "./config";

@Injectable({
  providedIn: "root"
})
export class FlightEmailSubscriptionService {
  constructor(private http: HttpClient, private config: Configuration) {}


  Delete(id: number): Observable<any> {
    return this.http
      .delete("https://dreamtravelsapi-demo.azurewebsites.net" + "/api/FlightEmailSubscription/" + id)
  }

  Insert(value: any): Observable<any> {
    return this.http
    .post("https://dreamtravelsapi-demo.azurewebsites.net" + "/api/FlightEmailSubscription", value, {
      observe: "body"
    })
  }

  GetByUserId(userId: number): Observable<IFlightEmailSubscription[]> {
    return this.http.get<IFlightEmailSubscription[]>(
      "https://dreamtravelsapi-demo.azurewebsites.net" + "/api/FlightEmailSubscription/" + userId,
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
