import { Injectable } from "@angular/core";
import { ICity } from "./city.service";

@Injectable({
  providedIn: "root"
})
export class DisplayService {
  map: google.maps.Map;
  directionsService = new google.maps.DirectionsService();

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

  displayPath(path) {
    var isToll = true;
    var roadColour = "#0080ff";

    if (path.OptimalCost > 0) {
      roadColour = "black";
      isToll = false;
    }

    var directionsDisplay = new google.maps.DirectionsRenderer({
      suppressMarkers: true,
      preserveViewport: true,
      polylineOptions: {
        strokeColor: roadColour,
        strokeWeight: 6,
        strokeOpacity: 0.6
      }
    });
    directionsDisplay.setMap(this.map);

    this.directionsService.route(
      {
        origin: new google.maps.LatLng(
          path.startingCity.latitude,
          path.startingCity.longitude
        ),
        destination: new google.maps.LatLng(
          path.endingCity.latitude,
          path.endingCity.longitude
        ),
        travelMode: google.maps.TravelMode.DRIVING,
        avoidTolls: isToll
      },
      (response, status) => {
        if (status === "OK") {
          directionsDisplay.setDirections(response);
        } else if (status === "OVER_QUERY_LIMIT") {
          console.log("Retrying display of path");

          setTimeout(() => {
            this.displayPath(path);
          }, 1000);

        } else {
          window.alert("Directions request failed due to " + status);
        }
      }
    );

    return directionsDisplay;
  }
}
