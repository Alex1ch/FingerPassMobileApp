using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Hardware.Fingerprints;
using Android.Support.V4;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Support.V4.Content;
using Android;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using Android.Telephony;
using Android.Content.PM;
using Android.Support.V4.App;
using System.IO;
using Java.Security;
using Android.Util;

namespace FingerPass
{
    [Activity(Label = "FingerPass", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        TcpClient client;

        public bool haveFPPermission(Context context) {
            Android.Content.PM.Permission permissionResult = ContextCompat.CheckSelfPermission(context, Manifest.Permission.UseFingerprint);
            if (permissionResult == Android.Content.PM.Permission.Granted)
                return true;
            else
                return false;
        }
        
        public bool isScreenLocked() {
            KeyguardManager keyguardManager = (KeyguardManager)GetSystemService(KeyguardService);
            return keyguardManager.IsKeyguardSecure;
        }

        public bool isReady(ref string output) {
            if (FingerprintManagerCompat.From(this).IsHardwareDetected)
            {
                output += "Device is compatable,";
                if (isScreenLocked())
                {
                    output += "\nscreen locked,";
                    if (FingerprintManagerCompat.From(this).HasEnrolledFingerprints)
                    {
                        output += "\nhave fingerprints";
                        if (haveFPPermission(this))
                        {
                            output += "\nand permissions granted.";
                            return true;
                        }
                        else
                        {
                            output += "\nand permissions NOT granted";
                        }
                    }
                    else output += "\n but have NOT fingerprints";
                }
                else
                {
                    output += "\nbut screen NOT locked";
                }
            }
            else
            {
                output = "Device is NOT compatable!";
            }
            return false;
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            SetContentView(Resource.Layout.Main);
            
            
            Button assignButton = FindViewById<Button>(Resource.Id.Assign);
            Button auth = FindViewById<Button>(Resource.Id.Auth);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);

            info.Text = "";

            string output = "";
            if (isReady(ref output))
            {
                info.Text += "\nDevice is ready!";

                bool assigned;

                string filename = "user.config.cfg";
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);

                using (FileStream fs = File.Open(filePath, FileMode.OpenOrCreate)) {
                    try
                    {
                        StreamReader sr = new StreamReader(fs);
                        string login = sr.ReadLine();
                        info.Text += "\nLogin: " + login;
                        //info.Text += "\nServer Key: " + sr.ReadLine();
                        KeyStore sKeyStore = KeyStore.GetInstance("AndroidKeyStore");
                        sKeyStore.Load(null);
                        if (sKeyStore.ContainsAlias(login))
                        {   
                            info.Text += "\nDevice Key: "+Convert.ToBase64String(sKeyStore.GetCertificate(login).PublicKey.GetEncoded());
                        }
                        assigned = true;
                        info.Text += "\nAssigned";
                    }
                    catch (Exception e)
                    {
                        assigned = false;
                        info.Text += "\nNot assigned";
                    }
                }

                assignButton.Click += delegate {
                    var m_activity = new Intent(this, typeof(AssignActivity));
                    this.StartActivity(m_activity);
                };

                auth.Click += delegate {
                    var m_activity = new Intent(this, typeof(Auth));
                    this.StartActivity(m_activity);
                };
            }
            else
            {
                info.Text += output;
                info.Text += "\nDevice is NOT ready!";

                assignButton.Text = "@string/close";

                assignButton.Click += delegate {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                };
            }

        }
    }
}

