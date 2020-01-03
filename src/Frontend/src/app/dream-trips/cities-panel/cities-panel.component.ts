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
import { PathService } from "../path.service";
import { Configuration } from "../../config";

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
  isFetchingData: boolean = false;

  constructor(
    public cityService: CityService,
    private http: HttpClient,
    public pathService: PathService,
    private config: Configuration
  ) {}

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
    if (this.cityService.markers[index] != null) {
      this.cityService.markers[index].setMap(null);
    }
    this.cityService.markers[index] = null;
    this.cityService.cities[index] = null;

    this.citiesForm.removeControl(index);
    this.contorls = Object.keys(this.citiesForm.controls);
  }

  findAndDisplayCity(index) {
    this.isFetchingData = true;

    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    if (this.contorls.length + 1 <= this.cityService.cities.length) {
      this.addCity();
    }

    this.http
      .post<ICity>("https://dreamtravelsapi-demo.azurewebsites.net" + "/api/FindLocationOfCity", data, {
        observe: "body"
      })
      .subscribe(city => {
        this.cityService.updateCity(index, city, "✓");
        this.isFetchingData = false;
      });
  }

  runTSP() {
    this.isLoading = true;

    this.contorls.forEach(index => {
      if (this.citiesForm.controls[index].value == null) {
        this.removeCity(index);
      }
    });

    let data = {
      cities: this.cityService.cities,
      sessionId: 123
    };

    this.http
      .post<any[]>("https://dreamtravelsapi-demo.azurewebsites.net" + "/api/CalculateBestPath", data, {
        observe: "body"
      })
      .subscribe(pathList => {
        var noOfPaths = pathList.length;

        //Cities
        for (let i = 0; i <= noOfPaths; i++) {
          let city: ICity;

          if (i == noOfPaths) {
            //last city
            city = pathList[i - 1].endingCity;
          } else {
            city = pathList[i].startingCity;
          }

          this.cityService.updateCity(
            Number(this.contorls[i]),
            city,
            i.toString()
          );

          this.citiesForm.controls[this.contorls[i]].setValue(city.name);
        }

        //Paths
        this.pathService.clearPaths();

        for (let i = 0; i < noOfPaths; i++) {
          this.pathService.addPath(pathList[i]);
        }

        this.pathService.adjustBounds();
        this.contorls = Object.keys(this.citiesForm.controls);

        this.isLoading = false;
      });
  }
}
