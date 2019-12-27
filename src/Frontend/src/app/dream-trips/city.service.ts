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
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
