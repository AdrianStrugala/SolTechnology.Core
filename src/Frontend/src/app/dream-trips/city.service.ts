import { Injectable } from "@angular/core";
import { DisplayService } from "./display.service";
import { FormGroup, FormControl } from "@angular/forms";
import { HttpClient } from "@angular/common/http";

@Injectable({
  providedIn: "root"
})
export class CityService {
  public cities: ICity[] = [];
  public citiesForm = new FormGroup({
    0: new FormControl("")
  });
  public contorls = Object.keys(this.citiesForm.controls);

  public markers: any[] = [];
  public CityIndex: number = 0;

  public isFetchingData: boolean = false;

  constructor(
    private displayService: DisplayService,
    private http: HttpClient
  ) {}

  addCity() {
    this.CityIndex++;

    this.citiesForm.addControl(this.CityIndex.toString(), new FormControl());

    this.cities.push(null);
    this.markers.push(null);

    this.contorls = Object.keys(this.citiesForm.controls);
  }

  updateCity(index: number, city: ICity, label: string = index.toString()) {
    if (this.markers[index] != null) {
      this.markers[index].setMap(null);
    }

    this.citiesForm.controls[index].setValue(city.name);
    this.markers[index] = this.displayService.displayMarker(city, label);
    this.cities[index] = city;
  }

  removeCity(index) {
    if (this.markers[index] != null) {
      this.markers[index].setMap(null);
    }
    this.markers[index] = null;
    this.cities[index] = null;

    this.citiesForm.removeControl(index);

    this.contorls = Object.keys(this.citiesForm.controls);
  }

  findAndDisplayCity(index) {
    this.isFetchingData = true;

    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    if (
      this.contorls.length + 1 <=
      this.cities.length
    ) {
      this.addCity();
    }

    this.http
      .post<ICity>(
        "https://dreamtravelsapi-demo.azurewebsites.net" +
          "/api/FindLocationOfCity",
        data,
        {
          observe: "body"
        }
      )
      .subscribe(city => {
        this.updateCity(index, city, "✓");
        this.isFetchingData = false;
      });
  }

  addCityByMapClick(position) {
    this.isFetchingData = true;

    let data = {
      lat: position.lat(),
      lng: position.lng(),
      sessionId: 123
    };

    this.http
      .post<ICity>(
        "https://dreamtravelsapi-demo.azurewebsites.net" +
          "/api/FindNameOfCity",
        data,
        {
          observe: "body"
        }
      )
      .subscribe(city => {
        if (this.citiesForm.controls[this.CityIndex].value != null) {
          this.addCity();
        }
        this.updateCity(this.CityIndex, city, "✓");
        this.displayService.map.setCenter(
          this.markers[this.CityIndex].getPosition()
        );
        this.isFetchingData = false;
      });
  }
}

export interface ICity {
  id: number;
  name: string;
  latitude: string;
  longitude: string;
}
