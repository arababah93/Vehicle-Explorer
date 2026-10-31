import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Make, Model, PagedMakes, VehicleType } from './models';

@Injectable({ providedIn: 'root' })
export class VehicleApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getMakes(prefix: string, skip: number, take = 40): Observable<PagedMakes> {
    let params = new HttpParams().set('skip', skip).set('take', take);
    const filter = prefix.trim();
    if (filter) {
      params = params.set('prefix', filter);
    }

    return this.http.get<PagedMakes>(`${this.baseUrl}/makes`, { params });
  }

  getVehicleTypes(makeId: number): Observable<VehicleType[]> {
    return this.http.get<VehicleType[]>(`${this.baseUrl}/makes/${makeId}/vehicle-types`);
  }

  getModels(makeId: number, year: number, vehicleType?: string | null): Observable<Model[]> {
    let params = new HttpParams().set('year', year);
    if (vehicleType) {
      params = params.set('vehicleType', vehicleType);
    }

    return this.http.get<Model[]>(`${this.baseUrl}/makes/${makeId}/models`, { params });
  }
}
