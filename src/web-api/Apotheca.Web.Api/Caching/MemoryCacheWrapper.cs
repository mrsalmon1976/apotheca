using Microsoft.Extensions.Caching.Memory;

namespace Apotheca.Web.Api.Caching
{

    public  interface IMemoryCacheWrapper
    {
        T Get<T>(string cacheKey);

        void Set<T>(string cacheKey, T cachedItem, TimeSpan slidingExpiration);
    }

    public class MemoryCacheWrapper : IMemoryCacheWrapper
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheWrapper(IMemoryCache memoryCache)
        {
            this._memoryCache = memoryCache;
        }

        public virtual T Get<T>(string cacheKey)
        {
            return _memoryCache.Get<T>(cacheKey);
        }

        public virtual void Set<T>(string cacheKey, T cachedItem, TimeSpan slidingExpiration)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
            options.SlidingExpiration = slidingExpiration;
            _memoryCache.Set(cacheKey, cachedItem, options);
        }
    }
}
