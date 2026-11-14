import type { ServiceRequest } from '../models/serviceRequest';

const shortDate = new Intl.DateTimeFormat('es-CO', {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
});
const fullDate = new Intl.DateTimeFormat('es-CO', { dateStyle: 'long', timeStyle: 'medium' });

interface RequestsTableProps {
  requests: ServiceRequest[];
}

/** Reference `table.tbl.stack`: becomes labeled stacked rows at ≤ 640 px. "Actualizada" is the server `updatedAt`. */
export function RequestsTable({ requests }: RequestsTableProps) {
  return (
    <div className="tbl-wrap">
      <table className="tbl stack">
        <caption>Más recientes primero · la lista se actualiza sola mientras el procesador avanza</caption>
        <thead>
          <tr>
            <th scope="col">Referencia externa</th>
            <th scope="col">Cliente</th>
            <th scope="col">Servicio</th>
            <th scope="col">Estado</th>
            <th scope="col" className="r">
              Actualizada
            </th>
          </tr>
        </thead>
        <tbody>
          {requests.map((request) => {
            const updatedAt = new Date(request.updatedAt);
            return (
              <tr key={request.id}>
                <td data-label="Referencia">
                  <div>
                    <span className="ref">{request.externalReference}</span>
                    <span className="desc">{request.description}</span>
                  </div>
                </td>
                <td data-label="Cliente" className="nowrap">
                  {request.customerId}
                </td>
                <td data-label="Servicio" className="nowrap">
                  {request.serviceCode}
                </td>
                <td data-label="Estado">
                  <span className={`badge badge-${request.status.toLowerCase()}`}>{request.status}</span>
                </td>
                <td data-label="Actualizada" className="r">
                  <time dateTime={request.updatedAt} title={fullDate.format(updatedAt)}>
                    {shortDate.format(updatedAt)}
                  </time>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
