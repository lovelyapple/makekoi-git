using System;

namespace Makekoi.PartnerCreate
{
    public interface IPartnerCreateModel
    {
        DatePartnerData CreatePartnerData();
    }

    public sealed class PartnerCreateModel : IPartnerCreateModel
    {
        private readonly Random _random;

        public PartnerCreateModel()
        {
            _random = new Random();
        }

        public DatePartnerData CreatePartnerData()
        {
            var partList = BuildPartData();
            var focusPart = partList[_random.Next(partList.Length)].PartType;
            var gender = (GenderType)_random.Next(0, 2);

            return new DatePartnerData(gender, partList, focusPart);
        }

        private PartnerPartData[] BuildPartData()
        {
            var partTypes = (PartType[])Enum.GetValues(typeof(PartType));
            var list = new PartnerPartData[partTypes.Length];

            for (var i = 0; i < partTypes.Length; i++)
            {
                var score = _random.Next(1, 16);
                list[i] = new PartnerPartData(partTypes[i], score);
            }

            return list;
        }
    }
}