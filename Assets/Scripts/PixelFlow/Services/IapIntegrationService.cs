using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using PixelFlow.Data;
using UnityEngine;
using UnityEngine.Scripting;

namespace PixelFlow.Services
{
    public class IapIntegrationService : INexusService
    {
        [Inject] public IIapService IapService { get; set; }
        [Inject] public IEconomyService EconomyService { get; set; }
        [Inject] public ILoggerService Logger { get; set; }
        [Inject] public IFeedbackService FeedbackService { get; set; }
        [Inject, OptionalInject] public GameConfig Config { get; set; }

        private EconomyConfigAsset _economyConfig;
        private bool _productsRegistered;
        private const string CoinCurrencyId = "coin";
        private const string GemCurrencyId = "gem";
        private const string TicketCurrencyId = "ticket";

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            return default;
        }

        public void RegisterEconomyConfig(EconomyConfigAsset config)
        {
            _economyConfig = config;
            if (!_productsRegistered && _economyConfig != null)
            {
                RegisterProducts();
            }
        }

        private void RegisterProducts()
        {
            if (_economyConfig?.IapProducts == null || _economyConfig.IapProducts.Count == 0)
            {
                Logger?.LogWarning("[PixelFlow.IapIntegrationService] No IAP products defined in EconomyConfigAsset.");
                return;
            }

            var productDefinitions = new List<Nexus.Core.Services.ProductDefinition>();

            foreach (var product in _economyConfig.IapProducts)
            {
                if (string.IsNullOrEmpty(product.ProductId))
                {
                    Logger?.LogWarning($"[PixelFlow.IapIntegrationService] Skipping product with empty ProductId: {product.DisplayName}");
                    continue;
                }

                var nexusType = product.Type switch
                {
                    IapProductType.Consumable => Nexus.Core.Services.ProductType.Consumable,
                    IapProductType.NonConsumable => Nexus.Core.Services.ProductType.NonConsumable,
                    IapProductType.Subscription => Nexus.Core.Services.ProductType.Subscription,
                    _ => Nexus.Core.Services.ProductType.Consumable
                };

                var def = new Nexus.Core.Services.ProductDefinition
                {
                    Id = product.ProductId,
                    Type = nexusType,
                    PriceString = $"${product.PriceUsd:F2}"
                };

                productDefinitions.Add(def);
            }

            IapService.RegisterProducts(productDefinitions.ToArray());
            _productsRegistered = true;
            Logger?.Log($"[PixelFlow.IapIntegrationService] Registered {productDefinitions.Count} IAP products with Nexus IapService.");
        }

        public void PurchaseProduct(string productId, Action<bool, string> onComplete)
        {
            if (!_productsRegistered)
            {
                Logger?.LogWarning($"[PixelFlow.IapIntegrationService] Attempting to purchase '{productId}' before products registered. Registering now...");
                RegisterProducts();
            }

            if (EconomyService == null)
            {
                Logger?.LogError("[PixelFlow.IapIntegrationService] EconomyService not available for purchase validation.");
                onComplete?.Invoke(false, "economy_unavailable");
                return;
            }

            var product = _economyConfig?.IapProducts.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
            {
                Logger?.LogWarning($"[PixelFlow.IapIntegrationService] Product not found in EconomyConfig: {productId}");
                onComplete?.Invoke(false, "product_not_found");
                return;
            }

            Logger?.Log($"[PixelFlow.IapIntegrationService] Initiating purchase: {product.DisplayName} ({productId})");

            IapService.PurchaseProduct(productId, (success, receipt) =>
            {
                if (!success)
                {
                    Logger?.LogWarning($"[PixelFlow.IapIntegrationService] Purchase failed for {productId}: {receipt}");
                    onComplete?.Invoke(false, receipt);
                    FeedbackService?.Play(FeedbackPreset.ErrorFailure);
                    return;
                }

                // Grant rewards based on product type
                GrantProductRewards(product);
                
                Logger?.Log($"[PixelFlow.IapIntegrationService] Purchase successful: {productId}");
                onComplete?.Invoke(true, receipt);
                FeedbackService?.Play(FeedbackPreset.SuccessFanfare);
            });
        }

        private void GrantProductRewards(IapProductDefinition product)
        {
            if (product.CoinAmount > 0 && EconomyService != null)
            {
                EconomyService.Earn(CoinCurrencyId, product.CoinAmount, $"iap_reward:{product.ProductId}");
            }
            if (product.GemAmount > 0 && EconomyService != null)
            {
                EconomyService.Earn(GemCurrencyId, product.GemAmount, $"iap_reward:{product.ProductId}");
            }
            if (!string.IsNullOrEmpty(product.UnlockSkinId) && EconomyService is PixelFlow.Services.LocalEconomyValidator localValidator)
            {
                // Note: Skin unlocking is handled via SkinUnlockCommand via signal
                Logger?.Log($"[PixelFlow.IapIntegrationService] Skin unlock product purchased: {product.UnlockSkinId}");
            }
            if (product.RemovesAds && EconomyService is PixelFlow.Services.LocalEconomyValidator)
            {
                Logger?.Log($"[PixelFlow.IapIntegrationService] 'No Ads' product purchased: {product.ProductId}");
            }
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            IapService.RestorePurchases(onComplete);
        }

        public bool IsProductOwned(string productId)
        {
            return IapService.IsProductOwned(productId);
        }

        public void OnDispose() { }
    }
}