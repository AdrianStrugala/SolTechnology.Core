import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, Subject } from "rxjs";
import { tap } from "rxjs/operators";

@Injectable({
  providedIn: "root"
})
export class CityService {

  public Cities: ICity[] = [];
  public Cities$: Subject<ICity[]>;

  constructor() {
  }
}

export class ICity {
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
