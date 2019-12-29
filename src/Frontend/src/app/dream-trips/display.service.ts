import { Injectable } from "@angular/core";
import { ICity, CityService } from "./city.service";

@Injectable({
  providedIn: "root"
})
export class DisplayService {


  map: google.maps.Map;

  constructor() {}

  displayMarker(city: ICity, label: string) {
    let marker = new google.maps.Marker({
      position: {
        lat: city.latitude,
        lng: city.longitude
      },
      map: this.map,
      draggable: true,
      label: {
        text: label,
        color: "white",
        fontWeight: "bold"
      }
    } as any);

    this.map.setCenter(marker.getPosition());

    return marker;
  }


}
