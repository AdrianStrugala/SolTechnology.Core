import {
  Component,
  ViewChildren,
  QueryList,
  ElementRef,
  AfterViewInit
} from "@angular/core";
import { CityService, ICity } from "../city.service";
import { HttpClient } from "@angular/common/http";
import { FormGroup, FormControl } from "@angular/forms";
import { DisplayService } from "../display.service";
import { BehaviorSubject } from "rxjs";

@Component({
  selector: "app-cities-panel",
  templateUrl: "./cities-panel.component.html",
  styleUrls: ["./cities-panel.component.scss"]
})
export class CitiesPanelComponent implements AfterViewInit {
  @ViewChildren("cityRows") cityRows: QueryList<ElementRef>;

  citiesForm = new FormGroup({
    0: new FormControl("")
  });

  contorls = Object.keys(this.citiesForm.controls);
  isLoading: boolean = false;

  constructor(public cityService: CityService, private http: HttpClient) {}

  ngAfterViewInit(): void {
    //Focus on the last row in Cities Panel
    this.cityRows.changes.subscribe(() => {
      this.cityRows.last.nativeElement.children[0].children[1].focus();
    });
  }

  addCity() {
    this.citiesForm.addControl(
      this.cityService.CityIndex.toString(),
      new FormControl()
    );
    this.contorls = Object.keys(this.citiesForm.controls);

    this.cityService.cities.push(null);
    this.cityService.markers.push(null);

    this.cityService.CityIndex++;
  }

  removeCity(index) {
    if ( this.cityService.markers[index] != null) {
      this.cityService.markers[index].setMap(null);
    }
    this.cityService.markers.splice(index, 1);
    this.cityService.cities.splice(index, 1);

    this.citiesForm.removeControl(index);
    this.contorls = Object.keys(this.citiesForm.controls);
  }

  findAndDisplayCity(index) {
    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    if (this.contorls.length + 1 <= this.cityService.cities.length) {
      this.addCity();
    }

    this.http
      .post<ICity>("http://localhost:53725/api/FindLocationOfCity", data, {
        observe: "body"
      })
      .subscribe(city => {
        this.cityService.updateCity(index, city, "✓");
      });
  }

  runTSP() {
    this.isLoading = true;

    let data = {
      cities: this.cityService.cities,
      sessionId: 123
    };

    console.log(this.citiesForm.controls)

    this.http
      .post<any[]>("http://localhost:53725/api/CalculateBestPath", data, {
        observe: "body"
      })
      .subscribe(pathList => {
        var noOfPaths = pathList.length;

        for (let i = 0; i < noOfPaths; i++) {
          this.cityService.updateCity(i, pathList[i].startingCity);
          this.citiesForm.controls[i].setValue(pathList[i].startingCity.name);
        }
        //last city
        this.cityService.updateCity(
          noOfPaths,
          pathList[noOfPaths - 1].endingCity
        );
        this.citiesForm.controls[noOfPaths].setValue(
          pathList[noOfPaths - 1].endingCity.name
        );
        console.log( this.citiesForm.controls[noOfPaths - 1])
        this.contorls = Object.keys(this.citiesForm.controls);
        this.isLoading = false;
      });
  }
}
