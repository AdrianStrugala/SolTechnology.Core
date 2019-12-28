import { Component, ViewChildren, QueryList, ElementRef, ViewChild, AfterViewInit } from "@angular/core";
import { CityService, ICity } from "../city.service";
import { tap } from "rxjs/operators";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {
  FormGroup,
  FormControl,
  Validators,
  AbstractControl
} from "@angular/forms";
import { MarkerService } from "../marker.service";

@Component({
  selector: "app-cities-panel",
  templateUrl: "./cities-panel.component.html",
  styleUrls: ["./cities-panel.component.scss"]
})
export class CitiesPanelComponent implements AfterViewInit {
  ngAfterViewInit(): void {
    this.addCity();

    console.log(this.cityRows);
  }

  // @ViewChild('cityRows', {static: false}) cityRows: ElementRef;
  @ViewChildren('cityRows') cityRows: ElementRef;

  citiesForm = new FormGroup({});

  contorls = Object.keys(this.citiesForm.controls);

  constructor(
    public cityService: CityService,
    private http: HttpClient
  ) {}

  addCity() {
    this.citiesForm.addControl(
      this.cityService.CityIndex.toString(),
      new FormControl()
    );
    this.contorls = Object.keys(this.citiesForm.controls);

    this.cityService.CityIndex++;

    console.log(this.cityRows);
    // this.cityRows.last.nativeElement.focus();
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
