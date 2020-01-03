import { Injectable } from "@angular/core";
import { DisplayService } from "./display.service";
import { CityService } from "./city.service";

@Injectable({
  providedIn: "root"
})
export class PathService {
  public paths = [];

  constructor(
    private displayService: DisplayService,
    private cityService: CityService
  ) {}

  clearPaths() {
    for (var i = 0; i < this.paths.length; i++) {
      this.paths[i].setMap(null);
    }
    this.paths = [];
  }

  addPath(path) {
    this.paths.push(
      this.displayService.displayPath(path)
    );
  }

  adjustBounds() {
    var bounds = new google.maps.LatLngBounds();
    for (var i = 0; i < this.cityService.markers.length; i++) {
      if (this.cityService.markers[i] != null) {
        bounds.extend(this.cityService.markers[i].position);
      }
    }
    this.displayService.map.fitBounds(bounds);
  }
}

export interface ICity {
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
