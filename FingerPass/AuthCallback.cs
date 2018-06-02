using System.Threading.Tasks;
using Android.Support.V4.Hardware.Fingerprint;
using Java.Lang;

namespace FingerPass
{
    class AuthCallback : FingerprintManagerCompat.AuthenticationCallback
    {
        Auth authActivity;

        public AuthCallback(Auth activity)
        {
            authActivity = activity;
        }

        public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
        {
            authActivity.RunOnUiThread(()=> { authActivity.Message.Text = "Личность подтверждена, \nвыполняется аутентификация..."; });
            Task.Factory.StartNew(() => {
                if (authActivity.Authenticate())
                {
                }
                authActivity.RunOnUiThread(() => {
                    authActivity.Message.Text = authActivity.Output;
                    authActivity.AuthButton.Visibility = Android.Views.ViewStates.Visible;
                    authActivity.AuthButton.Enabled = true;
                });
                authActivity.Active = false;

            });
        }

        public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
        {
            // Report the error to the user. Note that if the user canceled the scan,
            // this method will be called and the errMsgId will be FingerprintState.ErrorCanceled.
            authActivity.Active = false;
            authActivity.RunOnUiThread(() => {
                authActivity.AuthButton.Visibility = Android.Views.ViewStates.Visible;
                authActivity.AuthButton.Enabled = true;
                authActivity.Message.Text = "Ошибка при распознавании отпечатка пальца,\nпопробуйте позже";
            });
        }

        public override void OnAuthenticationFailed()
        {
            // Tell the user that the fingerprint was not recognized.
            authActivity.RunOnUiThread(() => { authActivity.Message.Text = "Не удалось распознать отпечаток пальца"; });
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            // Notify the user that the scan failed and display the provided hint.
            authActivity.RunOnUiThread(() => {
                authActivity.Message.Text = "Не удалось сканировать отпечаток пальца";
            });
        }
    }
}