import type { PipelineStage } from '../types';

interface PipelineStatusPanelProps {
  stages: PipelineStage[];
  retryCount: number;
  isVisible: boolean;
}

function SpinnerIcon() {
  return (
    <svg
      className="w-4 h-4 text-accent animate-spin"
      fill="none"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <circle
        className="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        strokeWidth="3"
      />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
      />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg
      className="w-4 h-4 text-green-500"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
    </svg>
  );
}

function ErrorIcon() {
  return (
    <svg
      className="w-4 h-4 text-red-500"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
  );
}

function PendingIcon() {
  return (
    <svg
      className="w-4 h-4 text-neutral-600"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <circle cx="12" cy="12" r="10" strokeWidth={2} />
    </svg>
  );
}

function StageIndicator({ stage }: { stage: PipelineStage }) {
  const icon =
    stage.state === 'active' ? <SpinnerIcon /> :
    stage.state === 'completed' ? <CheckIcon /> :
    stage.state === 'error' ? <ErrorIcon /> :
    <PendingIcon />;

  const labelColor =
    stage.state === 'active' ? 'text-accent' :
    stage.state === 'completed' ? 'text-neutral-400' :
    stage.state === 'error' ? 'text-red-500' :
    'text-neutral-600';

  return (
    <div className="flex items-center gap-2 min-w-0">
      <div className="shrink-0">{icon}</div>
      <div className="min-w-0">
        <span className={`text-xs font-medium ${labelColor}`}>{stage.label}</span>
        {stage.state === 'active' && stage.detail && (
          <p className="text-[10px] text-neutral-500 truncate">{stage.detail}</p>
        )}
        {stage.state === 'completed' && stage.detail && (
          <p className="text-[10px] text-neutral-600 truncate">{stage.detail}</p>
        )}
      </div>
    </div>
  );
}

function Connector({ state }: { state: 'pending' | 'reached' }) {
  return (
    <div
      className={`hidden sm:block h-px flex-1 min-w-3 max-w-8 ${
        state === 'reached' ? 'bg-neutral-600' : 'bg-neutral-800'
      }`}
    />
  );
}

export function PipelineStatusPanel({ stages, retryCount, isVisible }: PipelineStatusPanelProps) {
  return (
    <div
      className={`overflow-hidden transition-all duration-300 ease-in-out ${
        isVisible
          ? 'max-h-40 opacity-100'
          : 'max-h-0 opacity-0'
      }`}
      role="status"
      aria-label="Pipeline progress"
    >
      <div className="px-4 py-3 bg-surface border-b border-neutral-800 font-mono">
        <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-0">
          {stages.map((stage, i) => (
            <div key={stage.node} className="flex items-center gap-0 sm:flex-1 min-w-0">
              <StageIndicator stage={stage} />
              {i < stages.length - 1 && (
                <Connector
                  state={stage.state === 'completed' || stage.state === 'active' ? 'reached' : 'pending'}
                />
              )}
            </div>
          ))}
        </div>
        {retryCount > 0 && (
          <p className="text-[10px] text-amber-500 mt-2">
            Retry {retryCount} of 2
          </p>
        )}
      </div>
    </div>
  );
}
