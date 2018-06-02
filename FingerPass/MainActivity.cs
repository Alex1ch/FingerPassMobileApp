using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Support.V4.Content;
using Android;
using System.Net.Sockets;
using System.IO;
[assembly: Application(Theme= "@android:style/Theme.Material.Light.DarkActionBar")]

namespace FingerPass
{
    [Activity(Label = "FingerPass", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
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
                output += "Устройство совместимо";
                if (isScreenLocked())
                {
                    output += "\nЭкран заблокирован";
                    if (FingerprintManagerCompat.From(this).HasEnrolledFingerprints)
                    {
                        output += "\nЕсть зарегистрированные отпечатки";
                        if (haveFPPermission(this))
                        {
                            output += "\nРазрешения получены";
                            return true;
                        }
                        else
                        {
                            output += "\nНет разрешений";
                        }
                    }
                    else output += "\nНет зарегистрированных отпечатков";
                }
                else
                {
                    output += "\nЭкран без блокировки";
                }
            }
            else
            {
                output = "Устройство не совместимо";
            }
            return false;
        }


        protected override void OnResume()
        {
            base.OnResume();

            SetContentView(Resource.Layout.Main);
            
            Button assignButton = FindViewById<Button>(Resource.Id.Assign);
            Button auth = FindViewById<Button>(Resource.Id.Auth);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);

            info.Text = "";

            string output = "";
            if (isReady(ref output))
            {
                auth.Visibility = Android.Views.ViewStates.Visible;
                auth.Enabled = true;
                info.Text += "\nУстройство готово к использованию!";


                string filename = "username";
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);
                
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open))
                    {
                        StreamReader sr = new StreamReader(fs);
                        string login = sr.ReadLine();
                        info.Text += "\nПользователь: " + login;
                        if (login.Length > 1)
                        {
                            info.Text += "\nЗарегистрировано";
                            assignButton.Text = "Зарегистрировать повторно";
                            auth.Enabled = true;
                        }
                        else
                        {
                            info.Text += "\nНе зарегистрировано";
                            assignButton.Text = "Зарегистрировать";
                            auth.Enabled = false;
                        }
                    }
                }
                catch
                {
                    info.Text += "\nНе зарегистрировано";
                    assignButton.Text = "Зарегистрировать";
                    auth.Enabled = false;
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

                auth.Visibility = Android.Views.ViewStates.Invisible;
                auth.Enabled = false;
                assignButton.Text = "Закрыть";


                assignButton.Click += delegate {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                };
            }
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            SetContentView(Resource.Layout.Main);


            Button assignButton = FindViewById<Button>(Resource.Id.Assign);
            Button auth = FindViewById<Button>(Resource.Id.Auth);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);
            

            string output = "";
            if (isReady(ref output))
            {
                auth.Visibility = Android.Views.ViewStates.Visible;
                auth.Enabled = true;
                info.Text = "\nУстройство готово к использованию!";


                string filename = "username";
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);

                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open))
                    {
                        StreamReader sr = new StreamReader(fs);
                        string login = sr.ReadLine();
                        info.Text += "\nПользователь: " + login;
                        if (login.Length > 1)
                        {
                            info.Text += "\nЗарегистрировано";
                            assignButton.Text = "Зарегистрировать повторно";
                            auth.Enabled = true;
                        }
                        else
                        {
                            info.Text += "\nНе зарегистрировано";
                            assignButton.Text = "Зарегистрировать";
                            auth.Enabled = false;
                        }
                    }
                }
                catch
                {
                    info.Text += "\nНе зарегистрировано";
                    assignButton.Text = "Зарегистрировать";
                    auth.Enabled = false;
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

                auth.Visibility = Android.Views.ViewStates.Invisible;
                auth.Enabled = false;
                assignButton.Text = "Закрыть";

                assignButton.Click += delegate {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                };
            }

        }
    }
}

