export const GA_MEASUREMENT_ID = import.meta.env.VITE_GA_MEASUREMENT_ID as string | undefined;

declare global {
  interface Window { dataLayer?: any[] }
}

export function isEnabled() {
  return typeof GA_MEASUREMENT_ID === 'string' && GA_MEASUREMENT_ID.length > 0;
}

export function pageview(path: string) {
  if (!isEnabled()) return;
  window.dataLayer = window.dataLayer || [];
  // @ts-ignore
  window.dataLayer.push({ event: 'pageview', page_path: path });
}

export function event(name: string, params?: Record<string, any>) {
  if (!isEnabled()) return;
  window.dataLayer = window.dataLayer || [];
  // @ts-ignore
  window.dataLayer.push({ event: name, ...params });
}
