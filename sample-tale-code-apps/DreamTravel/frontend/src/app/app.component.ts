import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css', './external-libraries/bootstrap.css']
})
export class AppComponent {
  public constructor(private titleService: Title) {
    this.titleService.setTitle("Dream Travels");
  }

}
