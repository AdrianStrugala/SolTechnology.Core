import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";

@Injectable({
  providedIn: "root"
})
export class AirportsService {
  constructor(private http: HttpClient) {}

  airports$: Observable<IAirport[]> = this.http
    .get<IAirport[]>(
      APPLICATION_URL + "/api/airports"
    )
    .pipe(
      map(val => val || []),
      publishReplay(1),
      refCount()
    );
}

export interface IAirport {
  id: number;
  name: string;
  codes: string[];
}
