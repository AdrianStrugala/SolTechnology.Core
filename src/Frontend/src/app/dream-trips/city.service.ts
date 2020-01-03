import { Injectable } from "@angular/core";
import { DisplayService } from "./display.service";

@Injectable({
  providedIn: "root"
})
export class CityService {
  public cities: ICity[] = [];
  public markers: any[] = [];
  public CityIndex: number = 1;

  constructor(private displayService: DisplayService) {}

  updateCity(index: number, city: ICity, label: string = index.toString()) {
    if (this.markers[index] != null) {
      this.markers[index].setMap(null);
    }

    this.markers[index] = this.displayService.displayMarker(city, label);
    this.cities[index] = city;
  }
}

export interface ICity {
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
