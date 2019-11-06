import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService{

  
  public user : IUser

  constructor(private http: HttpClient) { 
    this.user = {} as IUser;
    this.user.Id = 0;

    if(localStorage.getItem("user") != null){
      this.user = JSON.parse(localStorage.getItem("user"));
    }
  }

  url = "https://dreamtravelsapi.azurewebsites.net/api/login";

  login(): Observable<any> {

  return this.http.post(
      this.url,
      this.user,
      {
          observe: "body"
      });
  } 

  isLoggedIn() : boolean{
    if(this.user.Id != 0){
      return true;
    }
    else{
      return false;
    }
  }

  logout() {
    this.user = {} as IUser;
    this.user.Id = 0;
  }
}

export interface IUser { 
  Id: number;
  Name: string;
  Password: string;
  Email: string;
}