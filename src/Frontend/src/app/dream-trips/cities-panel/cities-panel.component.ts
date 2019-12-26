import { Component } from "@angular/core";
import { CityService, ICity } from "../city.service";

@Component({
  selector: "app-cities-panel",
  templateUrl: "./cities-panel.component.html",
  styleUrls: ["./cities-panel.component.scss"]
})
export class CitiesPanelComponent {
  someCity: ICity;
  dupa: ICity[];

  constructor(public cityService: CityService) {
    this.someCity = new ICity;
    this.someCity.id = 0;
    this.someCity.name = "Wroclaw";

    // const xd = [this.someCity];

    // cityService.Cities$.next(xd);

    cityService.Cities.push(this.someCity);
  }
}
