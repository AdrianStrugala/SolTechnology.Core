import { Component, AfterViewInit, ViewChild, ElementRef } from
'@angular/core';

@Component({
  selector: "app-map",
  templateUrl: "./map.component.html",
  styleUrls: ["./map.component.scss"]
})


export class MapComponent implements AfterViewInit {


  @ViewChild('mapContainer', {static: false}) gmap: ElementRef;

  map: google.maps.Map;

  mapOptions: google.maps.MapOptions = {
    center: { lat: 0, lng: 0 },
    zoom: 4,
  };

  constructor() {}

  ngAfterViewInit(): void {
    this.map = new google.maps.Map(this.gmap.nativeElement,
      this.mapOptions);
  }
}
