import type { ReactNode } from 'react';

export type AlertTone = 'normal' | 'informational' | 'warning' | 'error';

interface FeedbackAlertProps {
  tone: AlertTone;
  title: string;
  children?: ReactNode;
  /** Short response label, e.g. "201 Created". */
  code?: string;
  action?: { label: string; onClick: () => void };
}

/** Reference `.alert-*`: success/info are announced politely, warnings/errors assertively. */
export function FeedbackAlert({ tone, title, children, code, action }: FeedbackAlertProps) {
  const role = tone === 'normal' || tone === 'informational' ? 'status' : 'alert';
  return (
    <div className={`alert alert-${tone}`} role={role}>
      <span>
        <strong>{title}</strong> {children}
      </span>
      {action ? (
        <button className="link" type="button" onClick={action.onClick}>
          {action.label}
        </button>
      ) : (
        code && <span className="code">{code}</span>
      )}
    </div>
  );
}
