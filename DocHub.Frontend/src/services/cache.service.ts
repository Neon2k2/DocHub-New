interface CacheItem<T> {
  data: T;
  timestamp: number;
  ttl: number; // fresh window
  staleTtl: number; // stale-while-revalidate window
}

type Fetcher<T> = () => Promise<T>;

class CacheService {
  private cache = new Map<string, CacheItem<any>>();
  private inFlight = new Map<string, Promise<any>>();
  private readonly DEFAULT_TTL = 5 * 60 * 1000; // 5 minutes fresh
  private readonly DEFAULT_STALE_TTL = 15 * 60 * 1000; // 15 minutes stale window

  set<T>(key: string, data: T, ttl?: number, staleTtl?: number): void {
    const item: CacheItem<T> = {
      data,
      timestamp: Date.now(),
      ttl: ttl ?? this.DEFAULT_TTL,
      staleTtl: staleTtl ?? this.DEFAULT_STALE_TTL
    };
    this.cache.set(key, item);
  }

  get<T>(key: string): T | null {
    const item = this.cache.get(key);
    if (!item) return null;

    const now = Date.now();
    if (now - item.timestamp > item.staleTtl) {
      this.cache.delete(key);
      return null;
    }

    return item.data as T;
  }

  isFresh(key: string): boolean {
    const item = this.cache.get(key);
    if (!item) return false;
    const now = Date.now();
    return now - item.timestamp <= item.ttl;
  }

  has(key: string): boolean {
    const item = this.cache.get(key);
    if (!item) return false;

    const now = Date.now();
    if (now - item.timestamp > item.staleTtl) {
      this.cache.delete(key);
      return false;
    }

    return true;
  }

  delete(key: string): void {
    this.cache.delete(key);
  }

  clear(): void {
    this.cache.clear();
  }

  invalidatePattern(pattern: string): void {
    const regex = new RegExp(pattern);
    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key);
      }
    }
  }

  getStats(): { size: number; keys: string[] } {
    return {
      size: this.cache.size,
      keys: Array.from(this.cache.keys())
    };
  }

  cleanExpired(): void {
    const now = Date.now();
    for (const [key, item] of this.cache.entries()) {
      if (now - item.timestamp > item.staleTtl) {
        this.cache.delete(key);
      }
    }
  }

  async getOrFetch<T>(
    key: string,
    fetcher: Fetcher<T>,
    options?: { ttl?: number; staleTtl?: number; revalidate?: boolean }
  ): Promise<T> {
    const existing = this.cache.get(key);
    const now = Date.now();
    const ttl = options?.ttl ?? this.DEFAULT_TTL;
    const staleTtl = options?.staleTtl ?? this.DEFAULT_STALE_TTL;

    if (existing) {
      const age = now - existing.timestamp;
      // Fresh: return immediately
      if (age <= existing.ttl) {
        return existing.data as T;
      }
      // Stale but within stale window: return stale and optionally revalidate
      if (age <= existing.staleTtl) {
        if (options?.revalidate !== false) {
          this.revalidate(key, fetcher, ttl, staleTtl);
        }
        return existing.data as T;
      }
      // Beyond stale: drop and fetch
      this.cache.delete(key);
    }

    // Deduplicate concurrent fetches
    const inFlight = this.inFlight.get(key);
    if (inFlight) return inFlight as Promise<T>;

    const promise = fetcher()
      .then((data) => {
        this.set(key, data, ttl, staleTtl);
        return data;
      })
      .finally(() => this.inFlight.delete(key));

    this.inFlight.set(key, promise);
    return promise as Promise<T>;
  }

  private async revalidate<T>(key: string, fetcher: Fetcher<T>, ttl: number, staleTtl: number) {
    if (this.inFlight.has(key)) return; // already revalidating
    const promise = fetcher()
      .then((data) => this.set(key, data, ttl, staleTtl))
      .finally(() => this.inFlight.delete(key));
    this.inFlight.set(key, promise);
  }
}

export const cacheService = new CacheService();

setInterval(() => {
  cacheService.cleanExpired();
}, 5 * 60 * 1000);
