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
    class AssignCallback : FingerprintManagerCompat.AuthenticationCallback
    {
        AssignActivity assignActivity;

        public AssignCallback(AssignActivity activity)
        {
            assignActivity = activity;
        }

        public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
        {
            assignActivity.RunOnUiThread(() => { assignActivity.info.Text = "Identity verified, performing assign..."; });
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
            assignActivity.info.Text = "Fingerprint authentication failed";
        }

        public override void OnAuthenticationFailed()
        {
            // Tell the user that the fingerprint was not recognized.
            assignActivity.info.Text = "Fingerprint wasn't recognize";
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            // Notify the user that the scan failed and display the provided hint.
            assignActivity.info.Text = "Fingerprint scan failed";
        }
    }
}