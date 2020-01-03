import { Injectable } from "@angular/core";
import { CityService, ICity } from "./city.service";
import { PathService } from "./path.service";
import { HttpClient } from "@angular/common/http";

@Injectable({
  providedIn: "root"
})
export class TSPService {
  public isLoading: boolean = false;

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
        this.pathService.clearPaths();

        for (let i = 0; i < noOfPaths; i++) {
          this.pathService.addPath(pathList[i]);
        }

        this.pathService.adjustBounds();
        this.cityService.contorls = Object.keys(this.cityService.citiesForm.controls);

        this.isLoading = false;
      });
  }
}
