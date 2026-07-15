using UnityEngine;

namespace Makekoi.PartnerCreate
{
    public enum PartGrade
    {
        Premium = 0,
        Mid = 1,
        Budget = 2,
    }

    public sealed class PartnerPartData
    {
        public GenderType Gender { get; }
        public PartType PartType { get; }
        public PartGrade Grade { get; }
        public Sprite PartSprite { get; }
        public PartsMaster PartMasterRecord { get; }

        public PartnerPartData(PartType partType, PartGrade grade, GenderType gender)
        {
            Gender = gender;
            PartType = partType;
            Grade = grade;
            PartMasterRecord = PartsTableContainer.GetRecord(partType, gender, grade);
            PartSprite = ResourceContainer.Instance.GetSpriteByPath(PartMasterRecord.TexturePath);
        }
    }
}