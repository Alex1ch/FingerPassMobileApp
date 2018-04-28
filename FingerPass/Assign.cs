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
using Android.Security.Keystore;
using Java.Security.Interfaces;
using Java.Security.Spec;
using Java.Math;
using Javax.Crypto;
using Android.Util;

namespace FingerPass
{
    [Activity(Label = "FingerPassAssign", Icon = "@mipmap/icon")]
    public class AssignActivity : Activity
    {
        static KeyPairGenerator sKeyPairGenerator;
        static Cipher sCipher;
        static string keyAlias;

        static bool GeyKeyPairGenerator() {
            try
            {
                sKeyPairGenerator = KeyPairGenerator.GetInstance("RSA", "AndroidKeyStore");
                return true;
            }
            catch  (KeyStoreException e){
                e.PrintStackTrace();
                return false;
            }
        }

        static string GenerateNewKey()
        {
            if (GeyKeyPairGenerator())
            {
                try
                {
                    sKeyPairGenerator.Initialize(new KeyGenParameterSpec.Builder(keyAlias,
                        KeyStorePurpose.Decrypt|KeyStorePurpose.Encrypt)
                            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingRsaOaep)
                            .SetDigests(KeyProperties.DigestSha256)
                            .SetUserAuthenticationRequired(false)
                            .SetKeySize(2048)
                            .Build());
                    
                    KeyPair keyPair = sKeyPairGenerator.GenerateKeyPair();

                    var publicKey = keyPair.Public;
                    
                    string publicKeyString = Base64.EncodeToString(publicKey.GetEncoded(), 0);

                    return publicKeyString;
                }
                catch (Exception e)
                {
                    var exception = String.Format("{0}\n{1}",e.Message,e.InnerException);
                }
            }
            return null;
        }

        private bool Assign(string login, string password, out SslStreamRW sslStreamRw, out string message)
        {
            sslStreamRw = null;
            message = "Error:\nUnknown reason";

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Denied)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadPhoneState }, 1);
            }

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Denied)
            {
                message = "Error:\nThis app require READ_PHONE_STATE permission to only get device IMEI";
                return false;
            }
            
            try
            {
                string result, server_rsa_open_key, device_rsa_open_key = "=====DEVICE RSA TEST KEY=====";
                TcpClient client = new TcpClient();
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


                if (!sslStreamRw.WriteString("<HANDSHAKE>")) return false;
                if (!sslStreamRw.WriteString(login)) return false;
                if (!sslStreamRw.WriteString(Build.Brand + " " + Build.Model)) return false;
                if (!sslStreamRw.WriteString(((TelephonyManager)GetSystemService(TelephonyService)).DeviceId)) { message = "Error:\nHaven't permissions"; return false; }//((TelephonyManager)GetSystemService(TelephonyService)).DeviceId)

                if (!sslStreamRw.WriteString(password)) return false;
                if (!sslStreamRw.ReadString(out server_rsa_open_key)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }

                keyAlias = login;

                device_rsa_open_key = GenerateNewKey();

                if (device_rsa_open_key!=null)
                {
                    if (!sslStreamRw.WriteString(device_rsa_open_key)) return false;
                }
                else
                {
                    message = "Error:\nCan't generate RSA key";
                    sslStreamRw.Disconnect("Device can't generate RSA key");
                    return false;
                };



                if (!sslStreamRw.ReadString(out result)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }
                if (result == "<ACCEPTED>")
                {
                    string filename = "user.config.cfg";
                    var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                    var filePath = Path.Combine(documentsPath, filename);
                    using (FileStream fs = File.Open(filePath, FileMode.Create))
                    {
                        StreamWriter sw = new StreamWriter(fs);

                        sw.WriteLine(login);
                        sw.WriteLine(server_rsa_open_key);
                        sw.Flush();
                    }

                    message = "Device is assigned";
                    sslStreamRw.Disconnect();
                    
                    return true;
                }
                else
                {
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
            SetContentView(Resource.Layout.Assign);



            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.SendAssign);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);
            EditText login = FindViewById<EditText>(Resource.Id.LoginEdit);
            EditText password = FindViewById<EditText>(Resource.Id.PasswordEdit);

            info.Text = "";

            string output = "";
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
                    if (Assign(assignLogin, assignPassword, out sslStreamRw, out message))
                    { }
                    info.Text = message;
                    //button.Enabled = false;
                });
            };

        }

    }
}