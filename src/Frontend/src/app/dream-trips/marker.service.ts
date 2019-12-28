import { Injectable, ViewChild, ElementRef } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, Subject, BehaviorSubject } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";
import { ICity, CityService } from "./city.service";

@Injectable({
  providedIn: "root"
})
export class MarkerService {
  markers: any[] = [];

  map: google.maps.Map;

  constructor(private cityService: CityService) {}

  displayMarker(city: ICity, label: string, index?: number) {
    console.log(city);
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

    console.log(index);
    if (index != null) {
      if (this.markers[index] != null) {
        this.markers[index].setMap(null);
      }
      this.markers[index] = marker;
    } else {
      this.markers.push(marker);
    }

    this.map.setCenter(marker.getPosition());
    console.log(this.markers.length)
  }

  updateCity(index: number, city: ICity, label = index) {
    this.displayMarker(city, label.toString(), index);
    this.cityService.Cities[index] = city;
  }
}
