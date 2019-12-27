import { Injectable } from "@angular/core";

@Injectable({
  providedIn: "root"
})
export class CityService {
  public Cities: ICity[] = [];
  public NumberOfCities: number = 0;

  constructor() {}
}

export class ICity {
  Id: number;
  Name: string;
  Latitude: string;
  Longitude: string;
}
