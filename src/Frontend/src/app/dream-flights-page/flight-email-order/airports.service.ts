import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AirportsService {

  constructor(private http: HttpClient) { }

  getAirports(): Observable<{}> {
    return this.http.get("https://dreamtravelsapi-demo.azurewebsites.net/api/airports");
  }
}



export interface IAirport { 
  id: number;
  name: string;
  codes: string[];
}