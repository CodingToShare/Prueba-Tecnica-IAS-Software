export const REQUEST_STATUSES = ['Pending', 'Processing', 'Completed', 'Failed'] as const;

export type ServiceRequestStatus = (typeof REQUEST_STATUSES)[number];

/** Mirrors ServiceRequestResponse returned by the API. */
export interface ServiceRequest {
  id: string;
  externalReference: string;
  customerId: string;
  serviceCode: string;
  description: string;
  status: ServiceRequestStatus;
  failureReason: string | null;
  createdAt: string;
  updatedAt: string;
}

/** Body of POST /api/requests. */
export interface CreateServiceRequestInput {
  externalReference: string;
  customerId: string;
  serviceCode: string;
  description: string;
}

export type CreateField = keyof CreateServiceRequestInput;

/** Same limits as the API/database columns (trimmed values). Convenience only: the server stays authoritative. */
export const MAX_LENGTHS: Record<CreateField, number> = {
  externalReference: 100,
  customerId: 50,
  serviceCode: 50,
  description: 1000,
};
