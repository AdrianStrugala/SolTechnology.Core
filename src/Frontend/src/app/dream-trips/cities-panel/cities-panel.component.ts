import { Component } from "@angular/core";
import { CityService, ICity } from "../city.service";
import { tap } from "rxjs/operators";

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

    // cityService.Cities$.pipe(
    //   tap(x => x.push(this.someCity))
    // )

    cityService.Cities.push(this.someCity);
  }

  addCity(){
    let xd = new ICity();
    xd.id = 2;
    xd.name = "Zadupie";

    this.cityService.Cities.push(xd);

    // cityService.Cities$.pipe(
    //   tap(x => x.push(this.someCity))
    // )

    console.log("add city")
  }
}
