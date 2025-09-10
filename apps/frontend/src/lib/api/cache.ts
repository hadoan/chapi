// Simple in-memory cache with deduping of inflight requests
const cache = new Map<string, any>();
const inflight = new Map<string, Promise<any>>();

export async function getOrFetch<T>(
  key: string,
  fetcher: () => Promise<T>
): Promise<T> {
  if (cache.has(key)) return cache.get(key) as T;
  if (inflight.has(key)) return inflight.get(key) as Promise<T>;
  const p = (async () => {
    try {
      const res = await fetcher();
      cache.set(key, res);
      return res;
    } finally {
      inflight.delete(key);
    }
  })();
  inflight.set(key, p);
  return p;
}

export function setCache<T>(key: string, value: T) {
  cache.set(key, value);
}

export function clearCache(key?: string) {
  if (key) cache.delete(key);
  else cache.clear();
}
