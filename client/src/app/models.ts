export interface Make {
  id: number;
  name: string;
}

export interface VehicleType {
  id: number;
  name: string;
}

export interface Model {
  id: number;
  name: string;
  makeId: number;
  makeName: string;
}

export interface PagedMakes {
  items: Make[];
  total: number;
}
