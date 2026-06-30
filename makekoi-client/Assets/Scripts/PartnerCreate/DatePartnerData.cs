namespace Makekoi.PartnerCreate
{
    public sealed class DatePartnerData
    {
        public GenderType Gender { get; }

        public PartnerPartData[] PartDataList { get; }

        public PartType FocusPart { get; }

        public DatePartnerData(GenderType gender, PartnerPartData[] partDataList, PartType focusPart)
        {
            Gender = gender;
            PartDataList = partDataList;
            FocusPart = focusPart;
        }
    }
}