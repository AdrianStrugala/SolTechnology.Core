import {
  Component,
  AfterViewInit,
  ViewChild,
  ElementRef,
  OnInit
} from "@angular/core";
import { MarkerService } from "../marker.service";
import { CityService } from "../city.service";

@Component({
  selector: "app-map",
  templateUrl: "./map.component.html",
  styleUrls: ["./map.component.scss"]
})
export class MapComponent implements AfterViewInit, OnInit {
  ngOnInit(): void {

    this.cityService.updated$.subscribe(x => {
     this.cityService.Cities.forEach(city => {
        let marker = new google.maps.Marker({
          position: {
            lat: city.Latitude,
            lng: city.Longitude
          },
          map: this.map,
          draggable: true,
          label: {
            text: "✓",
            color: "white",
            fontWeight: "bold"
          }
        } as any);

        this.markerService.markers.push(marker)
        this.map.setCenter(marker.getPosition());
      });
    });
  }

  @ViewChild("mapContainer", { static: false }) gmap: ElementRef;

  map: google.maps.Map;

  mapOptions: google.maps.MapOptions = {
    center: { lat: 0, lng: 0 },
    zoom: 4
  };

  constructor(private markerService: MarkerService, private cityService: CityService) {}

  ngAfterViewInit(): void {
    this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);
  }
}
