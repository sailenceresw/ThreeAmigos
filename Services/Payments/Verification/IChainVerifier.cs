namespace ecommerce.Services.Payments.Verification
{
    public interface IChainVerifier
    {
        string Currency { get; }

        Task<ChainVerificationResult> VerifyAsync(
            string txHash,
            string expectedAddress,
            decimal? expectedAmount,
            int minConfirmations,
            decimal amountTolerance,
            CancellationToken ct);
    }

    public interface IChainVerifierResolver
    {
        IChainVerifier? Resolve(string currency);
    }
}
