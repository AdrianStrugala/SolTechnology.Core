import {
  Component,
  AfterViewInit,
  ViewChild,
  ElementRef,
  OnInit
} from "@angular/core";
import { DisplayService } from "../display.service";
import { CityService, ICity } from "../city.service";
import { HttpClient } from "@angular/common/http";

@Component({
  selector: "app-map",
  templateUrl: "./map.component.html",
  styleUrls: ["./map.component.scss"]
})
export class MapComponent implements AfterViewInit {
  @ViewChild("mapContainer", { static: false }) gmap: ElementRef;

  mapOptions: google.maps.MapOptions = {
    center: { lat: 0, lng: 0 },
    zoom: 5
  };

  constructor(
    private markerService: DisplayService,
    private cityService: CityService,
    private http: HttpClient
  ) {}

  ngAfterViewInit(): void {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(position => {
        this.markerService.map.setCenter({
          lat: position.coords.latitude,
          lng: position.coords.longitude
        });
      });
    } else {
      console.log("Geolocation is not supported by the browser.");
    }

    this.markerService.map = new google.maps.Map(
      this.gmap.nativeElement,
      this.mapOptions
    );

    //Add marker by map click
    this.markerService.map.addListener("click", e => {
      this.cityService.addCityByMapClick(e.latLng);
    });
  }
}
