import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { DisplayService } from "./display.service";

@Injectable({
  providedIn: "root"
})
export class CityService {
  public Cities: ICity[] = [];
  public markers: any[] = [];
  public CityIndex: number = 1;

  constructor(private displayService: DisplayService) {}

  addCity(city: ICity) {
    this.Cities.push(city);

    this.markers.push(
      this.displayService.displayMarker(city, "✓")
    );
  }

  updateCity(index: number, city: ICity, label = index) {
    if (this.markers[index] != null) {
      this.markers[index].setMap(null);
    }

    this.markers[index] = this.displayService.displayMarker(city, label.toString());
    this.Cities[index] = city;
  }
}

export interface ICity {
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
