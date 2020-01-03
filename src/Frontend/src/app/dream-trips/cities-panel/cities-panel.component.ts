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

  contorls = Object.keys(this.cityService.citiesForm.controls);
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
    this.cityService.addCity();
    this.contorls = Object.keys(this.cityService.citiesForm.controls);
  }

  removeCity(index) {
    this.cityService.removeCity(index);
    this.contorls = Object.keys(this.cityService.citiesForm.controls);
  }

  findAndDisplayCity(index) {
    this.isFetchingData = true;

    let data = {
      name: this.cityService.citiesForm.controls[index].value,
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
      if (this.cityService.citiesForm.controls[index].value == null) {
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

          this.cityService.citiesForm.controls[this.contorls[i]].setValue(city.name);
        }

        //Paths
        this.pathService.clearPaths();

        for (let i = 0; i < noOfPaths; i++) {
          this.pathService.addPath(pathList[i]);
        }

        this.pathService.adjustBounds();
        this.contorls = Object.keys(this.cityService.citiesForm.controls);

        this.isLoading = false;
      });
  }
}
