import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class UserService{

  public user : IUser

  constructor(private http: HttpClient) {
    this.user = {} as IUser;
    this.user.id = 0;

    if(localStorage.getItem("user") != null){
      this.user = JSON.parse(localStorage.getItem("user"));
    }
  }

  url = "https://dreamtravelsapi-demo.azurewebsites.net";

  login(): Observable<any> {

  return this.http.post(
      this.url + "/api/login",
      this.user,
      {
          observe: "body"
      }).pipe(
        tap(user => localStorage.setItem("user", JSON.stringify(user)))
      );
  }

  register(user: IUser): Observable<any> {

    return this.http.post(
        this.url + "/api/register",
        user,
        {
            observe: "body"
        });
    }

  isLoggedIn() : boolean{
    if(this.user.id != 0){
      return true;
    }
    else{
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
