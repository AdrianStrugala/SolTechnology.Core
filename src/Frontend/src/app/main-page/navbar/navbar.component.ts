import { Component } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { LoginComponent } from "../login/login.component";
import { UserService } from "../../user.service";
import { Router } from "@angular/router";

@Component({
  selector: "navbar",
  templateUrl: "./navbar.component.html",
  styleUrls: ["./navbar.component.scss"]
})
export class NavbarComponent {
  navLinks: { label: string; link: string;}[];



  constructor(public dialog: MatDialog, public userService: UserService, private router: Router) {
    this.navLinks = [
      {
        label: 'Dream Flights',
        link: ''
      },
      {
        label: 'Register',
        link: './register'
      },
    ];
  }

  ngOnInit(): void {
    // this.router.events.subscribe((res) => {
    //   this.activeLinkIndex = this.navLinks.indexOf(this.navLinks.find(tab => tab.link === '.' + this.router.url));
    // });
  }

  openDialog(): void {
    if (this.dialog.openDialogs.length == 0) {
      this.dialog.open(LoginComponent, {
        height: "35rem",
        width: "30rem"
      });
    } else {
      this.dialog.closeAll();
    }
  }
}
