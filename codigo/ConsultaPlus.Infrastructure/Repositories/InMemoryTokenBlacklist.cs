using System;
using ConsultaPlus.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ConsultaPlus.Infrastructure.Security
{
    public class InMemoryTokenBlacklist : ITokenBlacklist
    {
        private readonly IMemoryCache _cache;
        public InMemoryTokenBlacklist(IMemoryCache cache) => _cache = cache;

        public void Add(string jti, DateTime expiresAtUtc) =>
            _cache.Set(jti, true, new MemoryCacheEntryOptions { AbsoluteExpiration = expiresAtUtc });

        public bool Contains(string jti) => _cache.TryGetValue(jti, out _);
    }
}
