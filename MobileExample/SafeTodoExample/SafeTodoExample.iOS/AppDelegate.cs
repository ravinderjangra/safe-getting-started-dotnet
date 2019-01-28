using System;
using System.Diagnostics;

using Foundation;
using SafeTodoExample.Service;
using UIKit;
using Xamarin.Forms;

namespace SafeTodoExample.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public AppService AppService => DependencyService.Get<AppService>();

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Rg.Plugins.Popup.Popup.Init();
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            return base.FinishedLaunching(app, options);
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            Device.BeginInvokeOnMainThread(
              async () =>
              {
                  try
                  {
                      await AppService.HandleUrlActivationAsync(url.ToString());
                      Debug.WriteLine("IPC Msg Handling Completed");
                  }
                  catch (Exception ex)
                  {
                      Debug.WriteLine($"Error: {ex.Message}");
                  }
              });
            return true;
        }
    }
}
