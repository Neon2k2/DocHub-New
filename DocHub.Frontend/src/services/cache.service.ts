interface CacheItem<T> {
  data: T;
  timestamp: number;
  ttl: number; // fresh window
  staleTtl: number; // stale-while-revalidate window
  priority: 'low' | 'normal' | 'high';
  tags: string[];
}

type Fetcher<T> = () => Promise<T>;

interface CacheOptions {
  ttl?: number;
  staleTtl?: number;
  priority?: 'low' | 'normal' | 'high';
  tags?: string[];
  revalidate?: boolean;
}

class CacheService {
  private cache = new Map<string, CacheItem<any>>();
  private inFlight = new Map<string, Promise<any>>();
  private readonly DEFAULT_TTL = 5 * 60 * 1000; // 5 minutes fresh
  private readonly DEFAULT_STALE_TTL = 15 * 60 * 1000; // 15 minutes stale window
  private readonly MAX_CACHE_SIZE = 1000; // Maximum number of items in cache
  private cleanupInterval: NodeJS.Timeout;

  constructor() {
    // Start cleanup interval
    this.cleanupInterval = setInterval(() => {
      this.cleanExpired();
    }, 60000); // Clean every minute
  }

  set<T>(key: string, data: T, ttl?: number, staleTtl?: number, options?: CacheOptions): void {
    // Check cache size limit
    if (this.cache.size >= this.MAX_CACHE_SIZE) {
      this.evictLeastRecentlyUsed();
    }

    const item: CacheItem<T> = {
      data,
      timestamp: Date.now(),
      ttl: ttl ?? this.DEFAULT_TTL,
      staleTtl: staleTtl ?? this.DEFAULT_STALE_TTL,
      priority: options?.priority ?? 'normal',
      tags: options?.tags ?? []
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
    options?: CacheOptions
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
          this.revalidate(key, fetcher, ttl, staleTtl, options);
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
        this.set(key, data, ttl, staleTtl, options);
        return data;
      })
      .finally(() => this.inFlight.delete(key));

    this.inFlight.set(key, promise);
    return promise as Promise<T>;
  }

  // New methods for enhanced caching
  setWithTags<T>(key: string, data: T, tags: string[], ttl?: number): void {
    this.set(key, data, ttl, undefined, { tags });
  }

  invalidateByTag(tag: string): void {
    for (const [key, item] of this.cache.entries()) {
      if (item.tags.includes(tag)) {
        this.cache.delete(key);
      }
    }
  }

  invalidateByPattern(pattern: string): void {
    const regex = new RegExp(pattern, 'i');
    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key);
      }
    }
  }

  getStats(): { size: number; keys: string[]; hitRate: number } {
    return {
      size: this.cache.size,
      keys: Array.from(this.cache.keys()),
      hitRate: 0 // Would need to track hits/misses for this
    };
  }

  private evictLeastRecentlyUsed(): void {
    // Sort by timestamp and remove oldest 10% of items
    const entries = Array.from(this.cache.entries());
    entries.sort((a, b) => a[1].timestamp - b[1].timestamp);
    
    const toRemove = Math.ceil(entries.length * 0.1);
    for (let i = 0; i < toRemove; i++) {
      this.cache.delete(entries[i][0]);
    }
  }

  private async revalidate<T>(key: string, fetcher: Fetcher<T>, ttl: number, staleTtl: number, options?: CacheOptions) {
    if (this.inFlight.has(key)) return; // already revalidating
    const promise = fetcher()
      .then((data) => this.set(key, data, ttl, staleTtl, options))
      .finally(() => this.inFlight.delete(key));
    this.inFlight.set(key, promise);
  }

  // Cleanup method
  destroy(): void {
    if (this.cleanupInterval) {
      clearInterval(this.cleanupInterval);
    }
  }
}

export const cacheService = new CacheService();

setInterval(() => {
  cacheService.cleanExpired();
}, 5 * 60 * 1000);
