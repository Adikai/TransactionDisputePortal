using Microsoft.AspNetCore.Components.Routing;

namespace TransactionDisputePortal.Client.Layout
{
    public partial class MainLayout : IDisposable
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

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            EvaluateRoute();
            await ValidateAuthenticationAsync();
            await InvokeAsync(StateHasChanged);
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
                var userID = await Auth.GetUserIDAsync();

                if (!userID.HasValue)
                {
                    isCheckingAuth = false;
                    Navigation.NavigateTo("login");
                    return;
                }

                var name = await Auth.GetCustomerNameAsync();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    customerName = name;
                }
            }

            isCheckingAuth = false;
            StateHasChanged();
        }

        private async Task HandleLogout()
        {
            await Auth.RemoveTokenAsync();
            customerName = "Portal User";
            Navigation.NavigateTo("login");
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}