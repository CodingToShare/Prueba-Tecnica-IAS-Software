import { REQUEST_STATUSES } from '../models/serviceRequest';
import type { StatusFilter } from '../hooks/useRequestList';

const OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: 'all', label: 'Todas' },
  ...REQUEST_STATUSES.map((status) => ({ value: status, label: status })),
];

interface RequestStatusFilterProps {
  value: StatusFilter;
  onChange: (value: StatusFilter) => void;
}

/** Reference segmented control; the selected option is exposed with aria-pressed. */
export function RequestStatusFilter({ value, onChange }: RequestStatusFilterProps) {
  return (
    <div className="segmented" role="group" aria-label="Filtrar por estado">
      {OPTIONS.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={option.value === value}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
