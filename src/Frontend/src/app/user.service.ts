import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  public Id : number;
  public Email: string;
  public Password : string;

  constructor(private http: HttpClient) { }

  url = "http://localhost:53725/api/login";

  login() {

   return this.http.post(
      this.url,
      [this.Email, this.Password],
      {
          observe: "body"
      })
      .subscribe();

  } 
}
