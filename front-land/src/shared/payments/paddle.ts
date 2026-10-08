import { paymentsApi } from '../api/paymentsApi';

/**
 * Lazy loader + initialiser for Paddle.js (v2). Loads the script once, initialises it
 * with the public client token from the backend, and fans Paddle checkout events out to
 * any subscribers. Kept dependency-free (Paddle typed loosely) so no extra npm package is
 * needed — the global `window.Paddle` is provided by the injected script.
 */

/* eslint-disable @typescript-eslint/no-explicit-any */
type Paddle = any;
export interface PaddleEvent { name?: string; data?: any; }
type Handler = (event: PaddleEvent) => void;

const PADDLE_JS_URL = 'https://cdn.paddle.com/paddle/v2/paddle.js';

let paddlePromise: Promise<Paddle> | null = null;
const handlers = new Set<Handler>();

/** Subscribe to Paddle checkout events. Returns an unsubscribe function. */
export function onPaddleEvent(handler: Handler): () => void {
    handlers.add(handler);
    return () => handlers.delete(handler);
}

function injectScript(): Promise<void> {
    return new Promise((resolve, reject) => {
        if ((window as any).Paddle) return resolve();
        const existing = document.querySelector<HTMLScriptElement>('script[data-paddle]');
        if (existing) {
            existing.addEventListener('load', () => resolve());
            existing.addEventListener('error', () => reject(new Error('Neuspešno učitavanje Paddle.js')));
            return;
        }
        const script = document.createElement('script');
        script.src = PADDLE_JS_URL;
        script.async = true;
        script.dataset.paddle = 'true';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Neuspešno učitavanje Paddle.js'));
        document.head.appendChild(script);
    });
}

/**
 * Returns an initialised Paddle instance, loading and configuring it on first call.
 * Throws if the backend reports payments are not configured.
 */
export async function getPaddle(): Promise<Paddle> {
    if (paddlePromise) return paddlePromise;

    paddlePromise = (async () => {
        const cfg = await paymentsApi.getPaddleConfig();
        if (!cfg.enabled || !cfg.clientToken) {
            throw new Error('Plaćanje trenutno nije dostupno.');
        }

        await injectScript();
        const Paddle = (window as any).Paddle;

        Paddle.Environment.set(cfg.environment === 'production' ? 'production' : 'sandbox');
        Paddle.Initialize({
            token: cfg.clientToken,
            eventCallback: (event: PaddleEvent) => {
                handlers.forEach((h) => h(event));
            },
        });

        return Paddle;
    })();

    // Reset on failure so a later call can retry (e.g. transient network error).
    paddlePromise.catch(() => { paddlePromise = null; });
    return paddlePromise;
}
