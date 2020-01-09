import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { tap } from "rxjs/operators";
import { Configuration } from "./config";

@Injectable({
  providedIn: "root"
})
export class UserService {
  public user: IUser;

  constructor(private http: HttpClient, private config: Configuration) {
    this.user = {} as IUser;
    this.user.id = 0;

    if (localStorage.getItem("user") != null) {
      this.user = JSON.parse(localStorage.getItem("user"));
    }
  }
  login(): Observable<any> {
    return this.http
      .post(this.config.APPLICATION_URL + "api/login", this.user, {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      })
      .pipe(tap(user => localStorage.setItem("user", JSON.stringify(user))));
  }

  register(user: IUser): Observable<any> {
    return this.http.post(this.config.APPLICATION_URL + "api/register", user, {
      observe: "body",
      headers: new HttpHeaders({
        Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
      })
    });
  }

  changePassword(currentPassword: string, password: string): Observable<any> {

    let data = {
      currentPassword: currentPassword,
      newPassword: password
    };

    return this.http
      .post(this.config.APPLICATION_URL + "api/changePassword", data, {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      });
  }

  isLoggedIn(): boolean {
    if (this.user.id != 0) {
      return true;
    } else {
      return false;
    }
  }

  logout() {
    this.user = {} as IUser;
    this.user.id = 0;

    localStorage.removeItem("user");

    window.location.reload();
  }
}

export interface IUser {
  id: number;
  name: string;
  password: string;
  email: string;
}
