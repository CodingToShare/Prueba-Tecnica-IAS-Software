import { useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { ApiError, createRequest, type ProblemDetails } from '../api/serviceRequestsApi';
import {
  MAX_LENGTHS,
  type CreateField,
  type CreateServiceRequestInput,
} from '../models/serviceRequest';
import { FeedbackAlert, type AlertTone } from './FeedbackAlert';

interface Feedback {
  tone: AlertTone;
  title: string;
  message: string;
  code?: string;
  retry?: boolean;
}

type FieldErrors = Partial<Record<CreateField, string>>;

const EMPTY: CreateServiceRequestInput = { externalReference: '', customerId: '', serviceCode: '', description: '' };

const FIELDS: { name: CreateField; label: string; placeholder: string; hint?: string; multiline?: boolean }[] = [
  {
    name: 'externalReference',
    label: 'Referencia externa',
    placeholder: 'p. ej. REQ-2026-0006',
    hint: 'Identifica la operación del cliente. Reenviarla no duplica la solicitud.',
  },
  { name: 'customerId', label: 'Cliente', placeholder: 'p. ej. CUS-1042' },
  { name: 'serviceCode', label: 'Código de servicio', placeholder: 'p. ej. MANT-PREV' },
  { name: 'description', label: 'Descripción', placeholder: 'Describe el servicio requerido', multiline: true },
];

/** Convenience checks only; the API remains authoritative (400 is still handled). */
function validate(values: CreateServiceRequestInput): FieldErrors {
  const errors: FieldErrors = {};
  for (const { name } of FIELDS) {
    const value = values[name].trim();
    if (!value) {
      errors[name] = 'Este campo es obligatorio.';
    } else if (value.length > MAX_LENGTHS[name]) {
      errors[name] = `Máximo ${MAX_LENGTHS[name]} caracteres.`;
    }
  }
  return errors;
}

/** Maps ASP.NET validation keys (e.g. "ExternalReference") to form fields. */
function fieldErrorsFromProblem(problem: ProblemDetails | null): FieldErrors {
  const errors: FieldErrors = {};
  for (const key of Object.keys(problem?.errors ?? {})) {
    const field = FIELDS.find((f) => f.name.toLowerCase() === key.toLowerCase());
    if (field) {
      errors[field.name] = 'El servicio rechazó este valor. Revísalo.';
    }
  }
  return errors;
}

interface CreateRequestFormProps {
  onRegistered: () => void;
}

export function CreateRequestForm({ onRegistered }: CreateRequestFormProps) {
  const [values, setValues] = useState<CreateServiceRequestInput>(EMPTY);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const inFlight = useRef(false);

  const change = (name: CreateField) => (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { value } = event.target;
    setValues((current) => ({ ...current, [name]: value }));
    setErrors((current) => ({ ...current, [name]: undefined }));
  };

  const submit = async () => {
    if (inFlight.current) {
      return;
    }
    const clientErrors = validate(values);
    setErrors(clientErrors);
    if (Object.keys(clientErrors).length > 0) {
      setFeedback({ tone: 'error', title: 'Revisa los campos marcados.', message: 'Hay datos faltantes o demasiado largos.' });
      return;
    }

    inFlight.current = true;
    setSubmitting(true);
    setFeedback(null);
    try {
      const { outcome, request } = await createRequest(values);
      setValues(EMPTY);
      setErrors({});
      setFeedback(
        outcome === 'created'
          ? {
              tone: 'normal',
              title: 'Solicitud registrada.',
              message: `${request.externalReference} quedó en estado ${request.status}.`,
              code: '201 Created',
            }
          : {
              tone: 'informational',
              title: `Ya existía una solicitud con la referencia ${request.externalReference}.`,
              message: `Se muestra la existente (estado ${request.status}); no se creó un duplicado.`,
              code: '200 OK',
            },
      );
      onRegistered();
    } catch (error) {
      setFeedback(feedbackForError(error, values.externalReference.trim()));
      if (error instanceof ApiError && error.status === 400) {
        setErrors(fieldErrorsFromProblem(error.problem));
      }
    } finally {
      inFlight.current = false;
      setSubmitting(false);
    }
  };

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void submit();
  };

  return (
    <section className="panel" aria-labelledby="new-request-title">
      <div className="panel-head">
        <h2 id="new-request-title">Nueva solicitud</h2>
        <span className="meta">Todos los campos son obligatorios</span>
      </div>
      <div className="panel-body">
        <form className="form-stack" onSubmit={onSubmit} noValidate>
          {FIELDS.map(({ name, label, placeholder, hint, multiline }) => {
            const error = errors[name];
            const describedBy = [hint ? `${name}-hint` : null, error ? `${name}-error` : null]
              .filter(Boolean)
              .join(' ') || undefined;
            const common = {
              id: name,
              name,
              value: values[name],
              placeholder,
              required: true,
              'aria-invalid': error ? true : undefined,
              'aria-describedby': describedBy,
              onChange: change(name),
            };
            return (
              <div key={name} className={error ? 'field error' : 'field'}>
                <label htmlFor={name}>{label}</label>
                {multiline ? <textarea {...common} /> : <input type="text" autoComplete="off" {...common} />}
                {hint && (
                  <div className="hint" id={`${name}-hint`}>
                    {hint}
                  </div>
                )}
                {error && (
                  <div className="hint" id={`${name}-error`}>
                    {error}
                  </div>
                )}
              </div>
            );
          })}
          <button className="btn btn-primary btn-block" type="submit" disabled={submitting}>
            {submitting && <span className="spinner" aria-hidden="true" />}
            {submitting ? 'Registrando…' : 'Registrar solicitud'}
          </button>
        </form>
        {feedback && (
          <div className="form-feedback">
            <FeedbackAlert
              tone={feedback.tone}
              title={feedback.title}
              code={feedback.code}
              action={feedback.retry ? { label: 'Reintentar', onClick: () => void submit() } : undefined}
            >
              {feedback.message}
            </FeedbackAlert>
          </div>
        )}
      </div>
    </section>
  );
}

function feedbackForError(error: unknown, reference: string): Feedback {
  if (error instanceof ApiError && error.status === 409) {
    return {
      tone: 'warning',
      title: `La referencia ${reference} ya existe con datos diferentes.`,
      message: 'Usa otra referencia o revisa la operación original.',
      code: '409 Conflict',
    };
  }
  if (error instanceof ApiError && error.status === 400) {
    return {
      tone: 'error',
      title: 'Revisa los campos marcados.',
      message: 'El servicio rechazó algunos datos.',
      code: '400 Bad Request',
    };
  }
  // Network failure, 5xx or anything unexpected: generic message, never server internals.
  return {
    tone: 'error',
    title: 'No se pudo contactar el servicio.',
    message: 'Tus datos se conservan en el formulario.',
    retry: true,
  };
}
