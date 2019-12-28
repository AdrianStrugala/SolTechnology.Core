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
import { MarkerService } from "../marker.service";

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

  constructor(public cityService: CityService, private http: HttpClient, private markerService: MarkerService) {}

  ngAfterViewInit(): void {
    //Focus on the last row in Cities Panel
    this.cityRows.changes.subscribe(() => {
      this.cityRows.last.nativeElement.children[0].children[1].focus();
    });
  }

  addCityRow() {
    this.citiesForm.addControl(
      this.cityService.CityIndex.toString(),
      new FormControl()
    );
    this.contorls = Object.keys(this.citiesForm.controls);

    this.cityService.CityIndex++;
  }

  findAndDisplayCity(index) {
    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    if (this.contorls.length + 1 <= this.cityService.Cities.length) {
      this.addCityRow();
    }

    this.http
      .post<ICity>("http://localhost:53725/api/FindLocationOfCity", data, {
        observe: "body"
      })
      .subscribe(city => {
        this.cityService.Cities.push(city),
          this.cityService.updated$.next(true);
      });
  }

  runTSP() {
    let data = {
      cities: this.cityService.Cities,
      sessionId: 123
    };

    this.http
      .post<any[]>("http://localhost:53725/api/CalculateBestPath", data, {
        observe: "body"
      })
      .subscribe(pathList => {

        var noOfPaths = pathList.length;

        for (let i = 0; i < noOfPaths; i++) {
          this.markerService.updateCity(i, pathList[i].startingCity);
          // this.cityService.Cities[i] = pathList[i].StartingCity
        }
        this.markerService.updateCity(noOfPaths, pathList[noOfPaths - 1].endingCity);
      });

    // $.ajax({
    //   type: "POST",
    //   dataType: "html",
    //   url: window.location + "api/CalculateBestPath",
    //   headers: {
    //     Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
    //   },
    //   data: {
    //     cities: cities,
    //     sessionId: sessionId,
    //     optimizePath: optimizeRoadChck
    //   },
    //   success(msg) {
    //     var pathList = JSON.parse(msg);
    //     displayPage(pathList, map);

    //     $("#pathsSummaryBtn")[0].style.display = "initial";
    //     $("#costLimiBtn")[0].style.display = "initial";
    //     $("#loader")[0].style.display = "none";
    //   },

    //   error(req, status, errorObj) {
    //     $("#loader")[0].style.display = "none";
    //     var alertMessage = JSON.parse(req.responseText);
    //     alert(alertMessage);
    //   }
    // });
  }
}
