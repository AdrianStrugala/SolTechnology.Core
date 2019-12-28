import { Injectable, ViewChild, ElementRef } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, Subject, BehaviorSubject } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";
import { ICity } from "./city.service";

@Injectable({
  providedIn: "root"
})
export class MarkerService {
  markers: any[] = [];

  map: google.maps.Map;

  constructor() {}

  displayMarker(city: ICity, label: string) {
    let marker = new google.maps.Marker({
      position: {
        lat: city.Latitude,
        lng: city.Longitude
      },
      map: this.map,
      draggable: true,
      label: {
        text: label,
        color: "white",
        fontWeight: "bold"
      }
    } as any);

    this.markers.push(marker);
    this.map.setCenter(marker.getPosition());
  }
}
