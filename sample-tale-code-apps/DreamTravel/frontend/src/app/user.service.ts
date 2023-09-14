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
    this.user.userId = '';

    if (localStorage.getItem("user") != null) {
      this.user = JSON.parse(localStorage.getItem("user"));
    }
  }
  login(): Observable<any> {
    return this.http
      .post(this.config.APPLICATION_URL + "api/users/login", this.user, {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      })
      .pipe(tap(user => localStorage.setItem("user", JSON.stringify(user))));
  }

  register(user: IUser): Observable<any> {
    return this.http.post(this.config.APPLICATION_URL + "api/users/register", user, {
      observe: "body",
      headers: new HttpHeaders({
        Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
      })
    });
  }

  changePassword(currentPassword: string, password: string): Observable<any> {

    let data = {
      currentPassword: currentPassword,
      newPassword: password,
      userId: this.user.userId
    };

    return this.http
      .post(this.config.APPLICATION_URL + "api/users/changePassword", data, {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      });
  }

  isLoggedIn(): boolean {
    if (this.user.userId != '') {
      return true;
    } else {
      return false;
    }
  }

  logout() {
    this.user = {} as IUser;
    this.user.userId = '';

    localStorage.removeItem("user");

    window.location.reload();
  }

  pay(amount : number): any {


console.log("dupa");

    let data = {
      userId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      amount: amount,
      currency: "DKK"
    };

console.log(data);

    this.http
      .post<IPayResponse>(this.config.APPLICATION_URL + "api/users/pay", data, {
        observe: "body",
        headers: new HttpHeaders({
          Authorization: "DreamAuthentication U29sVWJlckFsbGVz"
        })
      })  
      .subscribe(data => {
        console.log(data.authorizationUrl);
        window.location.href = data.authorizationUrl;
      });
  }
}

export interface IUser {
  userId: string;
  name: string;
  password: string;
  email: string;
}

export interface IPayResponse {
  authorizationUrl: string;
  paymentId: string;
}
