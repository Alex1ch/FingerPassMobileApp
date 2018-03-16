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

namespace FingerPass
{
    [Activity(Label = "FingerPass", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        TcpClient client;
        SslStream sslStream;

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

        private bool Handshake(string login, string password, out SslStreamRW sslStreamRw, out string message) {
            sslStreamRw=null;
            message = "Error:\nUnknown reason";
            try
            {
                string result, salt, rounds, server_rsa_open_key;
                string device_rsa_close_key = "=====DEVICE RSA TEST KEY=====";
                client = new TcpClient();
                client.SendTimeout = 10000;
                try
                {
                    client.Connect("fingerpass.ru", 6284);
                }
                catch
                {
                    message = "Error:\nCan't connect to server";
                    client.Close();
                    return false;
                }
                sslStreamRw = new SslStreamRW(client, "fingerpass.ru");


                //string pass_hash = HashFuncs.PBKDF2_SHA256_GetHash()

                if (!sslStreamRw.WriteString("<HANDSHAKE>")) return false;
                if (!sslStreamRw.WriteString(login)) return false;
                if (!sslStreamRw.WriteString(Build.Brand+" "+Build.Model)) return false;
                if (!sslStreamRw.ReadString(out salt)) { message = "Error:\n"+sslStreamRw.DisconnectionReason; return false; }
                if (!sslStreamRw.ReadString(out rounds)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }

                if (!sslStreamRw.WriteString(HashFuncs.PBKDF2Sha256GetBytes(32, password, salt, Int32.Parse(rounds)))) return false;
                if (!sslStreamRw.ReadString(out server_rsa_open_key)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }
                if (!sslStreamRw.WriteString(device_rsa_close_key)) return false;
                if (!sslStreamRw.ReadString(out result)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }
                if (result == "<ACCEPTED>")
                {
                    message = "Device is assigned";
                    sslStreamRw.Disconnect();
                    return true;
                }
                else {
                    sslStreamRw.DisconnectNoMessage();
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading message:\n" + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                sslStreamRw.DisconnectNoMessage();
                return false;
            }
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.CheckCompat);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);
            EditText login = FindViewById<EditText>(Resource.Id.LoginEdit);
            EditText password = FindViewById<EditText>(Resource.Id.PasswordEdit);

            info.Text = "";

            string output = "";
            if (isReady(ref output))
            {
                info.Text += output;
                info.Text += "\nDevice is ready!";

                button.Text = "Assign device";

                button.Click += delegate {
                    string assignLogin = login.Text;
                    string assignPassword = password.Text;
                    info.Text = "Loading...";
                    //button.Enabled = false;
                    Task.Factory.StartNew(() => {
                        SslStreamRW sslStreamRw;
                        string message;
                        if (Handshake(assignLogin, assignPassword, out sslStreamRw, out message))
                        { }
                        info.Text = message;
                        //button.Enabled = false;
                    });
                };

            }
            else
            {
                info.Text += output;
                info.Text += "\nDevice is NOT ready!";

                button.Text = "Exit";

                button.Click += delegate {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                };
            }

        }
    }
}

