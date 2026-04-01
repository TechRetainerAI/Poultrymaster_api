using User.Management.API.Models;

namespace User.Management.API.Billing
{
    public sealed record SubscriptionTierQuote(string TierKey, string ProductName, long UnitAmountMinor);

    public static class FarmSubscriptionPricing
    {
        public const int MaxBirdsAllowed = 1_000_000;

        public static SubscriptionTierQuote GetQuote(int totalBirdsActive, FarmSubscriptionOptions o)
        {
            if (o.Tier1MaxBirds < 0 || o.Tier2MaxBirds < o.Tier1MaxBirds)
                throw new InvalidOperationException("FarmSubscription tier limits are misconfigured.");

            var birds = Math.Max(0, totalBirdsActive);
            var currency = string.IsNullOrWhiteSpace(o.Currency) ? "ghs" : o.Currency.Trim().ToLowerInvariant();

            string tierKey;
            string productName;
            decimal amountMajor;

            if (birds <= o.Tier1MaxBirds)
            {
                tierKey = "tier1";
                productName = $"PoultryMaster — up to {o.Tier1MaxBirds:N0} birds (monthly)";
                amountMajor = o.Tier1MonthlyAmount;
            }
            else if (birds <= o.Tier2MaxBirds)
            {
                tierKey = "tier2";
                productName = $"PoultryMaster — {o.Tier1MaxBirds + 1:N0}–{o.Tier2MaxBirds:N0} birds (monthly)";
                amountMajor = o.Tier2MonthlyAmount;
            }
            else
            {
                tierKey = "tier3";
                productName = $"PoultryMaster — over {o.Tier2MaxBirds:N0} birds (monthly)";
                amountMajor = o.Tier3MonthlyAmount;
            }

            var minor = ToMinorUnits(amountMajor, currency);
            if (minor <= 0)
                throw new InvalidOperationException("Computed subscription amount must be greater than zero.");

            return new SubscriptionTierQuote(tierKey, productName, minor);
        }

        private static long ToMinorUnits(decimal major, string currency)
        {
            if (IsZeroDecimalCurrency(currency))
                return (long)Math.Round(major, MidpointRounding.AwayFromZero);
            return (long)Math.Round(major * 100m, MidpointRounding.AwayFromZero);
        }

        private static bool IsZeroDecimalCurrency(string c) =>
            c is "bif" or "clp" or "djf" or "gnf" or "jpy" or "kmf" or "krw" or "mga" or "pyg" or "rwf" or "ugx" or "vnd" or "vuv" or "xaf" or "xof" or "xpf";
    }
}
