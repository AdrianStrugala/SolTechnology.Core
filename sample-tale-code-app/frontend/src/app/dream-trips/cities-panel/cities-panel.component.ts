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
import { TSPService } from "../tsp.service";
import { UserService } from "../../user.service";

@Component({
  selector: "app-cities-panel",
  templateUrl: "./cities-panel.component.html",
  styleUrls: ["./cities-panel.component.scss"]
})
export class CitiesPanelComponent implements AfterViewInit {
  @ViewChildren("cityRows") cityRows: QueryList<ElementRef>;

  constructor(
    public cityService: CityService,
    private http: HttpClient,
    public pathService: PathService,
    private config: Configuration,
    public TSPService: TSPService,
    public UserService: UserService
  ) {}

  ngAfterViewInit(): void {
    //Focus on the last row in Cities Panel
    this.cityRows.changes.subscribe(() => {
      this.cityRows.last.nativeElement.children[0].children[0].focus();
    });
  }
}
