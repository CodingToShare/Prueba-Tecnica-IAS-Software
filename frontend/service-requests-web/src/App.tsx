import { useState } from 'react';
import { CreateRequestForm } from './components/CreateRequestForm';
import { FeedbackAlert } from './components/FeedbackAlert';
import { RequestStatusFilter } from './components/RequestStatusFilter';
import { RequestsTable } from './components/RequestsTable';
import { useRequestList, type StatusFilter } from './hooks/useRequestList';

export default function App() {
  const [filter, setFilter] = useState<StatusFilter>('all');
  const { requests, loading, failed, refresh } = useRequestList(filter);

  return (
    <>
      <a className="skip-link" href="#contenido">
        Saltar al contenido
      </a>
      <header className="brandbar">
        <div className="brand">
          <span className="logo" aria-hidden="true">
            PS
          </span>
          Portal de Solicitudes
        </div>
        <span className="context">Servicios en campo · Proveedor externo</span>
      </header>

      <main className="app-main" id="contenido" tabIndex={-1}>
        <div className="page-top">
          <div>
            <h1>Solicitudes de servicio</h1>
            <span className="who">Registra solicitudes y sigue su procesamiento con el proveedor externo</span>
          </div>
          <button className="btn btn-outline btn-sm" type="button" onClick={refresh}>
            <span aria-hidden="true">↻</span> Actualizar
          </button>
        </div>

        <div className="portal-grid">
          <CreateRequestForm onRegistered={refresh} />

          <section className="panel" aria-labelledby="requests-title">
            <div className="panel-head">
              <h2 id="requests-title">Solicitudes</h2>
              <RequestStatusFilter value={filter} onChange={setFilter} />
            </div>

            {failed && requests && (
              <div className="panel-body panel-alert">
                <FeedbackAlert
                  tone="error"
                  title="No se pudo actualizar la lista."
                  action={{ label: 'Reintentar', onClick: refresh }}
                >
                  Se muestran los últimos datos cargados.
                </FeedbackAlert>
              </div>
            )}

            {loading && <LoadingState />}
            {failed && !requests && <ErrorState onRetry={refresh} />}
            {requests && requests.length > 0 && <RequestsTable requests={requests} />}
            {requests && requests.length === 0 && (
              filter === 'all' ? <EmptyState /> : <NoResultsState filter={filter} onShowAll={() => setFilter('all')} />
            )}
          </section>
        </div>
      </main>
    </>
  );
}

function LoadingState() {
  return (
    <div className="state" aria-busy="true">
      <span className="sr-only">Cargando solicitudes…</span>
      <div className="skel skel-title" />
      <div className="skel" />
      <div className="skel skel-85" />
      <div className="skel skel-70" />
      <p>Cargando solicitudes</p>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="state">
      <div className="glyph glyph-brand" aria-hidden="true">
        +
      </div>
      <h3>Aún no hay solicitudes</h3>
      <p>Registra la primera con el formulario «Nueva solicitud».</p>
    </div>
  );
}

function NoResultsState({ filter, onShowAll }: { filter: StatusFilter; onShowAll: () => void }) {
  return (
    <div className="state">
      <div className="glyph glyph-muted" aria-hidden="true">
        ∅
      </div>
      <h3>Sin resultados</h3>
      <p>No hay solicitudes en estado «{filter}».</p>
      <button className="btn btn-outline btn-sm" type="button" onClick={onShowAll}>
        Ver todas
      </button>
    </div>
  );
}

function ErrorState({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="state" role="alert">
      <div className="glyph glyph-error" aria-hidden="true">
        !
      </div>
      <h3>No se pudieron cargar las solicitudes</h3>
      <p>Revisa la conexión e inténtalo de nuevo.</p>
      <button className="btn btn-secondary btn-sm" type="button" onClick={onRetry}>
        Reintentar
      </button>
    </div>
  );
}
