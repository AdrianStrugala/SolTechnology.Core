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

    this.cityService.CityIndex++;
  }

  findAndDisplayCity(index) {
    let data = {
      name: this.citiesForm.controls[index].value,
      sessionId: 123
    };

    if (this.contorls.length + 1 <= this.cityService.Cities.length) {
      this.addCity();
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
}
