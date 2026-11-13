namespace RippleGame.Attribution
{
    /// <summary>Attribution backend, supplied by an SDK-gated assembly. Called on the main thread.</summary>
    public interface IAttributionService
    {
        void Initialize(AttributionSettings settings);
        void LogAdRevenue(AttributionAdRevenue revenue);
    }
}
