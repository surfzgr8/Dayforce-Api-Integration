using IVCE.DAI.Domain.Models.Canonical;


namespace IVCE.DAI.Domain.Strategies
{
    public interface IStrategy
    {
        public bool? Validate(IList<CanonicalWorkerItem> canonicalWorkerItemList);
    }
}
