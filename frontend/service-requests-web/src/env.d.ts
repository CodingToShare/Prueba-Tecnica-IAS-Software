interface ImportMetaEnv {
  /** Base URL of ServiceRequests.Api, e.g. http://localhost:5164. Public value, never a secret. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
