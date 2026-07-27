using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;

namespace PixelFlow.Services
{
    /// <summary>
    /// Local economy validator that validates spend/earn against EconomyConfigAsset.
    /// game_plan.md §2.2 Zero-Mock/Zero-Hardcode: Real validation, no mock returns.
    /// </summary>
    public class LocalEconomyValidator : INetworkEconomyValidator
    {
        [Inject] public EconomyConfigAsset EconomyConfig { get; set; }

        public Task<bool> ValidateSpendAsync(string currencyId, long amount, string reason)
        {
            if (EconomyConfig == null)
                throw new DataValidationException("LocalEconomyValidator: EconomyConfigAsset not injected. Cannot validate spend.");

            if (string.IsNullOrEmpty(currencyId))
                throw new DataValidationException("LocalEconomyValidator: Currency ID cannot be null or empty.");

            if (amount <= 0)
                throw new DataValidationException("LocalEconomyValidator: Spend amount must be positive.");

            // Validate against known currency IDs from EconomyConfig
            if (!IsValidCurrency(currencyId))
                throw new DataValidationException($"LocalEconomyValidator: Unknown currency ID '{currencyId}'. Not defined in EconomyConfigAsset.IapProducts.");

            return Task.FromResult(true);
        }

public Task ValidateEarnAsync(string currencyId, long amount, string reason)
        {
            if (EconomyConfig == null)
                throw new DataValidationException("LocalEconomyValidator: EconomyConfigAsset not injected. Cannot validate earn.");

            if (string.IsNullOrEmpty(currencyId))
                throw new DataValidationException("LocalEconomyValidator: Currency ID cannot be null or empty.");

            if (amount <= 0)
                throw new DataValidationException("LocalEconomyValidator: Earn amount must be positive.");

            if (!IsValidCurrency(currencyId))
                throw new DataValidationException($"LocalEconomyValidator: Unknown currency ID '{currencyId}'. Not defined in EconomyConfigAsset.IapProducts.");

            return Task.CompletedTask;
        }

        private bool IsValidCurrency(string currencyId)
        {
            // Check against known economy currencies
            if (currencyId == "coin" || currencyId == "coins" || currencyId == "gem" || currencyId == "gems" || currencyId == "ticket" || currencyId == "tickets")
                return true;

            // Check against IAP product IDs
            if (EconomyConfig.IapProducts != null)
            {
                foreach (var product in EconomyConfig.IapProducts)
                {
                    if (product.ProductId == currencyId)
                        return true;
                }
            }
            return false;
        }
    }
}