import {
  Component,
  AfterViewInit,
  ViewChild,
  ElementRef,
  OnInit
} from "@angular/core";
import { MarkerService } from "../marker.service";

@Component({
  selector: "app-map",
  templateUrl: "./map.component.html",
  styleUrls: ["./map.component.scss"]
})
export class MapComponent implements AfterViewInit, OnInit {
  ngOnInit(): void {

    this.markerService.updated$.subscribe(x => {
      this.markerService.markers.forEach(marker => {
        let x = new google.maps.Marker({
          position: {
            lat: marker.latitude,
            lng: marker.longitude
          },
          map: this.map,
          draggable: true,
          label: {
            text: marker.label.toString(),
            color: "white",
            fontWeight: "bold"
          }
        } as any);

        this.map.setCenter(x.getPosition());
      });
    });
  }

  @ViewChild("mapContainer", { static: false }) gmap: ElementRef;

  map: google.maps.Map;

  mapOptions: google.maps.MapOptions = {
    center: { lat: 0, lng: 0 },
    zoom: 4
  };

  constructor(private markerService: MarkerService) {}

  ngAfterViewInit(): void {
    this.map = new google.maps.Map(this.gmap.nativeElement, this.mapOptions);
  }
}
