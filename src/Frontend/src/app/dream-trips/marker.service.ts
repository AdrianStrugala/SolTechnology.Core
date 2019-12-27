import { Injectable, ViewChild, ElementRef } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, Subject, BehaviorSubject } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";

@Injectable({
  providedIn: "root"
})
export class MarkerService {

  markers: IMarker[] = [];
  updated$ =  new BehaviorSubject<boolean>(false);

  constructor() {}
}

export interface IMarker {
  label: string;
  latitude: string;
  longitude: string;
}
