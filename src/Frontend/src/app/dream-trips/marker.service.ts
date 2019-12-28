import { Injectable, ViewChild, ElementRef } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, Subject, BehaviorSubject } from "rxjs";
import { publishReplay, map, refCount } from "rxjs/operators";

@Injectable({
  providedIn: "root"
})
export class MarkerService {

  markers: any[] = [];

  constructor() {}
}

