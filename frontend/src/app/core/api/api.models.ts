import { HttpParams } from '@angular/common/http';

/** The shape of every list endpoint in the API: { items, totalCount, page, pageSize }. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type SortDirection = 'asc' | 'desc';

/** Query-string parameters shared by every list endpoint. */
export interface ListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: SortDirection;
}

/**
 * Turns a query object into HTTP query parameters, skipping empty values,
 * so `{ page: 1, search: '' }` becomes `?page=1`.
 */
export function toHttpParams(query: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
