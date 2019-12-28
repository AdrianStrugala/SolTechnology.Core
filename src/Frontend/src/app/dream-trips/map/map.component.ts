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

        this.markerService.displayMarker(this.cityService.Cities[this.cityService.CityIndex-1], "✓")

    });
  }

  @ViewChild("mapContainer", { static: false }) gmap: ElementRef;

  mapOptions: google.maps.MapOptions = {
    center: { lat: 0, lng: 0 },
    zoom: 4
  };

  constructor(
    private markerService: MarkerService,
    private cityService: CityService
  ) {}

  ngAfterViewInit(): void {
    this.markerService.map = new google.maps.Map(
      this.gmap.nativeElement,
      this.mapOptions
    );
  }
}
