import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";

@Injectable({
  providedIn: "root"
})
export class CityService {
  public Cities: ICity[] = [];
  public CityIndex: number = 1;
  updated$ =  new BehaviorSubject<boolean>(false);

  constructor() {}
}

export class ICity {
  Id: number;
  Name: string;
  Latitude: string;
  Longitude: string;
}
