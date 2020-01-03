import { Injectable } from "@angular/core";

@Injectable({
  providedIn: "root"
})
export class Configuration {
  public APPLICATION_URL: 'https://dreamtravelsapi-demo.azurewebsites.net';

  constructor(
  ) {}
}
