import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError } from "rxjs/operators";
import { Configuration } from "./config";

@Injectable({
  providedIn: "root"
})
export class FlightEmailSubscriptionService {
  constructor(private http: HttpClient, private config: Configuration) {}

  Delete(id: number): Observable<any> {
    return this.http.delete(
      this.config.APPLICATION_URL + "api/FlightEmailSubscription/" + id,
      {
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      }
    );
  }

  Insert(value: any): Observable<any> {
    return this.http.post(
      this.config.APPLICATION_URL + "api/FlightEmailSubscription",
      value,
      {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      }
    );
  }

  GetByUserId(userId: number): Observable<IFlightEmailSubscription[]> {
    return this.http
      .get<IFlightEmailSubscription[]>(
        this.config.APPLICATION_URL + "api/FlightEmailSubscription/" + userId,
        {
          observe: "body",
          headers: new HttpHeaders({
            Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
          })
        }
      )
      .pipe(
        catchError(err => {
          console.error(err);
          return of([] as IFlightEmailSubscription[]);
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
