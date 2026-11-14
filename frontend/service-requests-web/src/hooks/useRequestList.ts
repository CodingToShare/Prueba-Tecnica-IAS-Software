import { useCallback, useEffect, useState } from 'react';
import { getRequests, isAbortError } from '../api/serviceRequestsApi';
import type { ServiceRequest, ServiceRequestStatus } from '../models/serviceRequest';

export type StatusFilter = ServiceRequestStatus | 'all';

/** Requests are processed in the background, so the list refreshes itself while the page is visible. */
export const REFRESH_INTERVAL_MS = 3000;

interface ListState {
  requests: ServiceRequest[] | null;
  /** Filter the rows in `requests` belong to. */
  loadedFilter: StatusFilter | null;
  /** Filter whose last load failed (null when the last load succeeded). */
  failedFilter: StatusFilter | null;
}

export interface RequestList {
  /** Rows for the active filter, or null when none have been loaded for it yet. */
  requests: ServiceRequest[] | null;
  /** First load for the active filter is in progress. */
  loading: boolean;
  /** Last load for the active filter failed; rows loaded before (if any) are kept. */
  failed: boolean;
  refresh: () => void;
}

export function useRequestList(filter: StatusFilter): RequestList {
  const [state, setState] = useState<ListState>({ requests: null, loadedFilter: null, failedFilter: null });
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;
    let timer: number | undefined;
    let controller: AbortController | undefined;

    // One request at a time: the next refresh is scheduled only after the current one settles.
    const load = async () => {
      if (document.visibilityState === 'hidden') {
        timer = window.setTimeout(load, REFRESH_INTERVAL_MS);
        return;
      }
      controller = new AbortController();
      try {
        const requests = await getRequests(filter === 'all' ? undefined : filter, controller.signal);
        if (!cancelled) {
          setState({ requests, loadedFilter: filter, failedFilter: null });
        }
      } catch (error) {
        if (cancelled || isAbortError(error)) {
          return;
        }
        setState((previous) => ({ ...previous, failedFilter: filter }));
      }
      if (!cancelled) {
        timer = window.setTimeout(load, REFRESH_INTERVAL_MS);
      }
    };

    void load();
    return () => {
      cancelled = true;
      controller?.abort();
      window.clearTimeout(timer);
    };
  }, [filter, reloadToken]);

  // A manual refresh/retry clears the failure so the user sees it working (skeleton when no rows are loaded yet).
  const refresh = useCallback(() => {
    setState((previous) => (previous.failedFilter === null ? previous : { ...previous, failedFilter: null }));
    setReloadToken((token) => token + 1);
  }, []);
  const requests = state.loadedFilter === filter ? state.requests : null;
  const failed = state.failedFilter === filter;

  return { requests, loading: requests === null && !failed, failed, refresh };
}
