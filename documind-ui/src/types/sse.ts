import type { Citation } from './message';

export interface SSETokenEvent {
  type: 'token';
  data: {
    content: string;
  };
}

export interface SSECitationEvent {
  type: 'citation';
  data: Citation;
}

export interface SSECompleteEvent {
  type: 'complete';
  data: {
    totalTokens?: number;
  };
}

export interface SSEErrorEvent {
  type: 'error';
  data: {
    message: string;
  };
}

export type SSEEvent = 
  | SSETokenEvent 
  | SSECitationEvent 
  | SSECompleteEvent 
  | SSEErrorEvent;
