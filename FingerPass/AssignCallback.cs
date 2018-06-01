using System.Threading.Tasks;
using Android.Support.V4.Hardware.Fingerprint;
using Java.Lang;

namespace FingerPass
{
    class AssignCallback : FingerprintManagerCompat.AuthenticationCallback
    {
        AssignActivity assignActivity;

        public AssignCallback(AssignActivity activity)
        {
            assignActivity = activity;
        }

        public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
        {
            assignActivity.RunOnUiThread(() => { assignActivity.info.Text = "Личность подтверждена, \nвыполняется регистрация..."; });
            Task.Factory.StartNew(() => {
                string message;
                if (assignActivity.Assign(out message))
                { }
                assignActivity.info.Text = message;
                assignActivity.Active = false;
            });
        }

        public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
        {
            // Report the error to the user. Note that if the user canceled the scan,
            // this method will be called and the errMsgId will be FingerprintState.ErrorCanceled.
            assignActivity.Active = false;
            assignActivity.info.Text = "Ошибка при распознавании отпечатка пальца,\nпопробуйте позже";
        }

        public override void OnAuthenticationFailed()
        {
            // Tell the user that the fingerprint was not recognized.
            assignActivity.info.Text = "Не удалось распознать отпечаток пальца";
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            // Notify the user that the scan failed and display the provided hint.
            assignActivity.info.Text = "Не удалось сканировать отпечаток пальца";
        }
    }
}