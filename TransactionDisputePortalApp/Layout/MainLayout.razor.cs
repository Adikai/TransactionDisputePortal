using Microsoft.AspNetCore.Components.Routing;

namespace TransactionDisputePortal.Client.Layout
{
    public partial class MainLayout
    {
        private bool isCheckingAuth = true;
        private bool isLoginPage = false;
        private string customerName = "Portal User";

        protected override void OnInitialized()
        {
            Navigation.LocationChanged += OnLocationChanged;
            EvaluateRoute();
        }

        private void EvaluateRoute()
        {
            var relativeUri = Navigation.ToBaseRelativePath(Navigation.Uri);
            isLoginPage = relativeUri.Equals("login", StringComparison.OrdinalIgnoreCase);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            EvaluateRoute();
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ValidateAuthenticationAsync();
            }
        }

        private async Task ValidateAuthenticationAsync()
        {
            isCheckingAuth = true;
            EvaluateRoute();

            if (!isLoginPage)
            {
                var customerId = await Auth.GetCustomerIdAsync();

                if (!customerId.HasValue)
                {
                    isCheckingAuth = false;
                    Navigation.NavigateTo("login");
                    return;
                }

                var name = await Auth.GetCustomerNameAsync();
                customerName = name;
            }

            isCheckingAuth = false;
            StateHasChanged();
        }

        private async Task HandleLogout()
        {
            await Auth.RemoveTokenAsync();
            Navigation.NavigateTo("login");
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}
