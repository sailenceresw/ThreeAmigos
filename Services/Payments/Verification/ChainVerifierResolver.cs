namespace ecommerce.Services.Payments.Verification
{
    public class ChainVerifierResolver : IChainVerifierResolver
    {
        private readonly IReadOnlyDictionary<string, IChainVerifier> _byCurrency;

        public ChainVerifierResolver(IEnumerable<IChainVerifier> verifiers)
        {
            _byCurrency = verifiers.ToDictionary(v => v.Currency, v => v, StringComparer.OrdinalIgnoreCase);
        }

        public IChainVerifier? Resolve(string currency)
        {
            return _byCurrency.TryGetValue(currency, out var v) ? v : null;
        }
    }
}
