using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using SafeTodoExample.Helpers;
using SafeTodoExample.ViewModel.Base;
using Xamarin.Forms;

namespace SafeTodoExample.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        public const string AuthInProgressMessage = "Connecting to SAFE Network...";

        public bool IsMock
        {
            get
            {
#if SAFE_APP_MOCK
                return true;
#else
                return false;
#endif
            }
        }

        public string BuildMode
        {
            get
            {
#if SAFE_APP_MOCK
                return "MOCK";
#else
                return "Non-Mock";
#endif
            }
        }

        public string WelcomeText
        {
            get
            {
#if SAFE_APP_MOCK
                return "You are running the mock build of the application. " +
                    "The button below will perform mock authentication for you.";
#else
                return "You are runnning the non-mock build of the application." +
                "Before hitting the Authenticate button please make sure that " +
                "you have your IP updated, SAFE Authenticator Application installed and you are logged in. You know the drill!";
#endif
            }
        }

        public ICommand MockConnectCommand => new Command(async () => await ConnectToMockAsync());

        public ICommand LiveConnectCommand => new Command(async () => await ConnectToLiveAsync());

        public async Task ConnectToMockAsync()
        {
            Debug.WriteLine("Initiate Mock Network Connection");
#if SAFE_APP_MOCK
            try
            {
                using (Acr.UserDialogs.UserDialogs.Instance.Loading("Authenticating"))
                {
                    await AppService.ProcessMockAuthentication();
                    MessagingCenter.Send(this, MessengerConstants.NavigateToItemPage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#else
            await Application.Current.MainPage.DisplayAlert("Configuration missing", "please add SAFE_APP_MOCK in build compilation symbols", "ok");
#endif
        }

        private async Task ConnectToLiveAsync()
        {
            Debug.WriteLine("Initiate Live Network Connection");

            try
            {
                DialogHelper.ShowToast(AuthInProgressMessage, DialogType.Information);
                var url = await AppService.GenerateAppRequestAsync();
                Device.BeginInvokeOnMainThread(() => { Device.OpenUri(new Uri(url)); });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
