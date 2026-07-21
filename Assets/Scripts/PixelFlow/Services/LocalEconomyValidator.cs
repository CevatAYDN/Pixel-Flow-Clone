using System.Threading.Tasks;
using Nexus.Core.Services;

namespace PixelFlow.Services
{
    public class LocalEconomyValidator : INetworkEconomyValidator
    {
        public Task<bool> ValidateSpendAsync(string currencyId, long amount, string reason)
        {
            return Task.FromResult(true);
        }

        public Task ValidateEarnAsync(string currencyId, long amount, string reason)
        {
            return Task.CompletedTask;
        }
    }
}
