import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";
import { Configuration } from "../config";

@Injectable({
  providedIn: "root"
})
export class AirportsService {
  constructor(private http: HttpClient, private config: Configuration) {}

  airports$: Observable<IAirport[]> = this.http
    .get<IAirport[]>(this.config.APPLICATION_URL + "api/airports")
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
