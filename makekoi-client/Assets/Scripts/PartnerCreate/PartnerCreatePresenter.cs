namespace Makekoi.PartnerCreate
{
    public sealed class PartnerCreatePresenter
    {
        private readonly IPartnerCreateModel _model;
        private readonly IPartnerCreateView _view;

        public PartnerCreatePresenter(IPartnerCreateModel model, IPartnerCreateView view)
        {
            _model = model;
            _view = view;
        }

        public void CreatePartner()
        {
            var partnerData = _model.CreatePartnerData();
            _view.ShowPartnerData(partnerData);
        }
    }
}