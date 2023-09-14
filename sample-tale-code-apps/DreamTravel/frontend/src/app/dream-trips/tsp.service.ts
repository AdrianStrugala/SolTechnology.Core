import { Injectable } from "@angular/core";
import { CityService, ICity } from "./city.service";
import { PathService } from "./path.service";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { BehaviorSubject } from "rxjs";
import { Configuration } from "../config";

@Injectable({
  providedIn: "root"
})
export class TSPService {
  public isLoading: boolean = false;
  public totalTime$ = new BehaviorSubject<string>("");
  public totalCost$ = new BehaviorSubject<number>(0);

  constructor(
    private pathService: PathService,
    private cityService: CityService,
    private http: HttpClient,
    private config: Configuration
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
        this.config.APPLICATION_URL + "api/CalculateBestPath",
        data,
        {
          observe: "body",
          headers: new HttpHeaders({
            Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
          })
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

          this.cityService.citiesForm.controls[
            this.cityService.contorls[i]
          ].setValue(city.name);
        }

        //Paths
        let totalTime = 0;
        let totalCost = 0;
        this.pathService.clearPaths();

        for (let i = 0; i < noOfPaths; i++) {
          totalTime += pathList[i].optimalDistance;
          totalCost += pathList[i].optimalCost;
          this.pathService.addPath(pathList[i]);
        }

        this.pathService.adjustBounds();
        this.cityService.contorls = Object.keys(
          this.cityService.citiesForm.controls
        );

        this.updateTimeString(totalTime);
        this.totalCost$.next(totalCost *7.46);

        this.isLoading = false;
      });
  }
  updateTimeString(totalTime: number) {
    var totalHours = Math.floor(totalTime / 3600);
    var totalMinutes = Math.floor(
      (totalTime - Math.floor(totalHours) * 3600) / 60
    );
    var totalSeconds = totalTime % 60;

    let result =
      Math.floor(totalHours) +
      ":" +
      this.pad2(Math.floor(totalMinutes)) +
      ":" +
      this.pad2(Math.floor(totalSeconds)) +
      ".";

    this.totalTime$.next(result);
  }

  pad2(number) {
    return (number < 10 ? "0" : "") + number;
  }
}
