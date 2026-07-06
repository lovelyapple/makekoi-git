namespace Makekoi.PartnerCreate
{
    public sealed class PartnerPartData
    {
        public PartType PartType { get; }

        public int AcquiredScore { get; }

        public PartnerPartData(PartType partType, int acquiredScore)
        {
            PartType = partType;
            AcquiredScore = acquiredScore;
        }
    }
}