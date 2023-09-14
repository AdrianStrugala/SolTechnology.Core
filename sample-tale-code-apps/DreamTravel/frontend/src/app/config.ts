import { Injectable } from "@angular/core";

@Injectable({
  providedIn: "root"
})

export class Configuration {
  public readonly APPLICATION_URL = "https://localhost:44330/";

  constructor(
    ) {}
}
