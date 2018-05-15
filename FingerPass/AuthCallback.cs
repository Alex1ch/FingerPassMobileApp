using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Javax.Crypto;
using static Android.Hardware.Fingerprints.FingerprintManager;

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
            authActivity.RunOnUiThread(()=> { authActivity.Message.Text = "Identity verified, performing auth..."; });
            Task.Factory.StartNew(() => {
                if (authActivity.Authenticate())
                {
                }
                if (authActivity.Rsa_device != null) authActivity.Rsa_device.Dispose();
                if (authActivity.Rsa_server != null) authActivity.Rsa_server.Dispose();
                authActivity.RunOnUiThread(() => { authActivity.Message.Text = authActivity.Output; });
                authActivity.Active = false;
                authActivity.AuthButton.Enabled = true;
            });
        }

        public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
        {
            // Report the error to the user. Note that if the user canceled the scan,
            // this method will be called and the errMsgId will be FingerprintState.ErrorCanceled.
            authActivity.Active = false;
            authActivity.AuthButton.Enabled = true;
            authActivity.RunOnUiThread(() => { authActivity.Message.Text = "Fingerprint authentication failed"; });
        }

        public override void OnAuthenticationFailed()
        {
            // Tell the user that the fingerprint was not recognized.
            authActivity.RunOnUiThread(() => { authActivity.Message.Text = "Fingerprint wasn't recognize"; });
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            // Notify the user that the scan failed and display the provided hint.
            authActivity.RunOnUiThread(() => {
                authActivity.Message.Text = "Fingerprint scan failed";
            });
        }
    }
}