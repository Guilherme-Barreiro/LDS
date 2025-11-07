using System;

namespace ConsultaPlus.Core.Interfaces
{
    public interface ITokenBlacklist
    {
        void Add(string jti, DateTime expiresAtUtc);
        bool Contains(string jti);
    }
}
