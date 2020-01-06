import { Injectable } from "@angular/core";
import { CityService, ICity } from "./city.service";
import { PathService } from "./path.service";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject } from "rxjs";

@Injectable({
  providedIn: "root"
})
export class TSPService {
  public isLoading: boolean = false;
  public totalTime$ = new BehaviorSubject<string>("");

  constructor(
    private pathService: PathService,
    private cityService: CityService,
    private http: HttpClient
  ) {}

  runTSP() {
    this.isLoading = true;

    this.cityService.contorls.forEach(index => {
      if (this.cityService.citiesForm.controls[index].value == null) {
        this.cityService.removeCity(index);
      }
    });

    let data = {
      cities: this.cityService.cities,
      sessionId: 123
    };

    this.http
      .post<any[]>(
        "https://dreamtravelsapi-demo.azurewebsites.net" +
          "/api/CalculateBestPath",
        data,
        {
          observe: "body"
        }
      )
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
            Number(this.cityService.contorls[i]),
            city,
            i.toString()
          );

          this.cityService.citiesForm.controls[this.cityService.contorls[i]].setValue(
            city.name
          );
        }

        //Paths
        let totalTime = 0;
        this.pathService.clearPaths();

        for (let i = 0; i < noOfPaths; i++) {
          totalTime += pathList[i].optimalDistance;
          this.pathService.addPath(pathList[i]);
        }

        this.pathService.adjustBounds();
        this.cityService.contorls = Object.keys(this.cityService.citiesForm.controls);

        this.updateTimeString(totalTime);

        this.isLoading = false;
      });
  }
  updateTimeString(totalTime: number) {
    var totalHours = Math.floor(totalTime / 3600);
    var totalMinutes = Math.floor((totalTime - Math.floor(totalHours) * 3600) / 60);
    var totalSeconds = (totalTime % 60);

    let result =  Math.floor(totalHours) +
    ":" +
    this.pad2(Math.floor(totalMinutes)) +
    ":" +
    this.pad2(Math.floor(totalSeconds)) +
    ".";

    this.totalTime$.next(result);
  }

  pad2(number) {
    return (number < 10 ? '0' : '') + number;
}
}
