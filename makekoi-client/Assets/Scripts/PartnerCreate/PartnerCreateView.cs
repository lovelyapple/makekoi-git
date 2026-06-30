using System.Text;
using UnityEngine;

namespace Makekoi.PartnerCreate
{
    public interface IPartnerCreateView
    {
        void ShowPartnerData(DatePartnerData partnerData);
    }

    public sealed class PartnerCreateView : MonoBehaviour, IPartnerCreateView
    {
        private PartnerCreatePresenter _presenter;

        private void Awake()
        {
            var model = new PartnerCreateModel();
            _presenter = new PartnerCreatePresenter(model, this);
        }

        private void Start()
        {
            _presenter.CreatePartner();
        }

        [ContextMenu("Create Partner")]
        private void CreatePartnerFromInspector()
        {
            _presenter.CreatePartner();
        }

        public void ShowPartnerData(DatePartnerData partnerData)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[MVP] Date Partner Created");
            sb.AppendLine($"Gender: {partnerData.Gender}");
            sb.AppendLine($"Focus Part: {partnerData.FocusPart}");
            sb.AppendLine("Part Scores:");

            foreach (var part in partnerData.PartDataList)
            {
                sb.AppendLine($"- {part.PartType}: {part.AcquiredScore}");
            }

            Debug.Log(sb.ToString());
        }
    }
}