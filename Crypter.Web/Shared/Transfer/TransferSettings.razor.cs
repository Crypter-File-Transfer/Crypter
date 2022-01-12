using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
    public partial class TransferSettingsBase : ComponentBase
    {
        private int _requestedExpirationHours;

        [Parameter]
        public int RequestedExpirationHours
        {
            get => _requestedExpirationHours;
            set
            {
                if (_requestedExpirationHours == value) return;
                if (value < 1) value = 1;
                if (value > 24) value = 24;
                _requestedExpirationHours = value;

                RequestedExpirationHoursChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<int> RequestedExpirationHoursChanged { get; set; }

        protected int requestedExpirationInHours = 24;

        protected override void OnInitialized()
        {
            RequestedExpirationHours = 24;
        }
    }
}
