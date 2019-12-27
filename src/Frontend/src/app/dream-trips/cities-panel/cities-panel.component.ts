import { Component } from "@angular/core";
import { CityService, ICity } from "../city.service";
import { tap } from "rxjs/operators";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {
  FormGroup,
  FormControl,
  Validators,
  AbstractControl
} from "@angular/forms";

@Component({
  selector: "app-cities-panel",
  templateUrl: "./cities-panel.component.html",
  styleUrls: ["./cities-panel.component.scss"]
})
export class CitiesPanelComponent {

  citiesForm = new FormGroup({
    0: new FormControl("")
  });

  contorls = Object.keys(this.citiesForm.controls);

  constructor(public cityService: CityService, private http: HttpClient) {
  }

  addCity() {
    this.citiesForm.addControl(this.cityService.NumberOfCities.toString(), new FormControl());
    this.contorls = Object.keys(this.citiesForm.controls);

    this.cityService.NumberOfCities++;
  }

  findAndDisplayCity(index) {
    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    this.http
      .post<ICity>("http://localhost:53725/api/FindLocationOfCity", data, {
        observe: "body"
      })
      .subscribe(city => {
        console.log(city),
          console.log(this.citiesForm),
          this.cityService.Cities.push(city),
          console.log(this.cityService.Cities),
          this.addCity();
      });
  }
}
