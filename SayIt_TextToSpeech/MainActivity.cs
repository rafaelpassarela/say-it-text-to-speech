using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SayIt_TextToSpeech
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener
    {
        private readonly int needLang = 103;
        private readonly int needConfig = 203;
        private Spinner spinLanguages;
        private Button btnShare;
        private Button btnSayIt;
        private Button btnClear;

        private AppController controller;
        private UtteranceProgressListenerWrapper listner;

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            // if get error, set default language
            if (status == OperationResult.Error)
                controller.SetDefaultLocale();

            // listener ok, set lang
            if (status == OperationResult.Success)
            {
                controller.InitializeLocaleList();

                // set up our spinner to the languages supported by the tts
                var adapter = new ArrayAdapter<string>(
                    this, Android.Resource.Layout.SimpleSpinnerDropDownItem, controller.LangAvailable);
                spinLanguages.Adapter = adapter;
                spinLanguages.SetSelection(controller.LangAvailable.IndexOf(controller.SelectedLocale.DisplayName));
                controller.UpdateTTSLanguage();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            // base.OnActivityResult(requestCode, resultCode, data);
            var localeAvailable = controller.IsLocaleAvailable();
            if (requestCode == needConfig || (requestCode == needLang && !localeAvailable))
            {
                // we need a new language installed
                var installTTS = new Intent();
                installTTS.SetAction(TextToSpeech.Engine.ActionInstallTtsData);
                StartActivity(installTTS);
            }
            else if (localeAvailable)
            {
                controller.UpdateTTSLanguage();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_git:
                    ShowGitPage();
                    return true;
                case Resource.Id.menu_config:
                    var checkTTSIntent = new Intent();
                    checkTTSIntent.SetAction(TextToSpeech.Engine.ActionCheckTtsData);
                    StartActivityForResult(checkTTSIntent, needConfig);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ShowGitPage()
        {
            var uri = Android.Net.Uri.Parse("https://goo.gl/muzv91");
            var intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            btnShare = FindViewById<Button>(Resource.Id.btnShare);
            btnClear = FindViewById<Button>(Resource.Id.btnClear);

            var editWhatToSay = FindViewById<EditText>(Resource.Id.editSpeech);
            var txtSpeedVal = FindViewById<TextView>(Resource.Id.textSpeed);
            var txtPitchVal = FindViewById<TextView>(Resource.Id.textPitch);
            var seekSpeed = FindViewById<SeekBar>(Resource.Id.seekSpeed);
            var seekPitch = FindViewById<SeekBar>(Resource.Id.seekPitch);
            spinLanguages = FindViewById<Spinner>(Resource.Id.spinLanguage);

            // create the TTS callback handler 
            listner = new UtteranceProgressListenerWrapper(
                (string x) =>
                {
                    //onDone
                    return true;
                },
                (string x) =>
                {
                    //onError
                    AlertDialog.InfoMessage(this, ":(", GetText(Resource.String.say_error), null);
                    return false;
                },
                (string x) =>
                {
                    //onBegin                   
                    return true;
                }
            );

            controller = new AppController(this, this, listner);

            // set up the initial pitch and speed values then the onscreen values
            // the pitch and rate both go from 0f to 1f, however if you have a seek bar with a max of 1, you get a single step
            // therefore, a simpler option is to have the slider go from 0 to 255 and divide the position of the slider by 255 to get
            // the float
            seekPitch.Progress = 254;
            seekSpeed.Progress = 254;
            txtPitchVal.Text = "1.00";
            txtSpeedVal.Text = "1.00";

            // connect up the events
            btnSayIt.Click += delegate
            {
                // if there is nothing to say, don't say it
                if (!string.IsNullOrEmpty(editWhatToSay.Text))
                    controller.Speak(editWhatToSay.Text);
                else
                    Toast.MakeText(this, GetText(Resource.String.no_text), ToastLength.Long).Show();
            };

            btnClear.Click += delegate
            {
                editWhatToSay.Text = "";
            };

            btnShare.Click += delegate
            {
                SaveAudio(editWhatToSay.Text);
            };

            // sliders
            seekPitch.StopTrackingTouch += (object sender, SeekBar.StopTrackingTouchEventArgs e) =>
            {
                var seek = sender as SeekBar;
                var progress = seek.Progress / 255f;

                controller.SetPitch(progress);

                txtPitchVal.Text = progress.ToString("F2");
            };

            seekSpeed.StopTrackingTouch += (object sender, SeekBar.StopTrackingTouchEventArgs e) =>
            {
                var seek = sender as SeekBar;
                var progress = seek.Progress / 255f;

                controller.SetSpeechRate(progress);

                txtSpeedVal.Text = progress.ToString("F2");
            };

            spinLanguages.ItemSelected += (object sender, AdapterView.ItemSelectedEventArgs e) =>
            {
                // find the selected locale
                controller.UpdateLocale((int)e.Id);

                // create intent to check the TTS has this language installed
                var checkTTSIntent = new Intent();
                checkTTSIntent.SetAction(TextToSpeech.Engine.ActionCheckTtsData);
                StartActivityForResult(checkTTSIntent, needLang);
            };           
        }

        private bool SetEnables(bool value)
        {
            btnClear.Enabled = value;
            btnSayIt.Enabled = value;
            btnShare.Enabled = value;
            return true;
        }

        private void SaveAudio(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                //btnShare.Enabled = false;

                //var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                //var fileName = GetText(Resource.String.say_button) + "_" + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".mp3";
                //var fullName = Path.Combine(path.AbsolutePath, fileName);

                //Java.IO.File javaFile = new Java.IO.File(fullName);
                //var res = textToSpeech.SynthesizeToFile(text, null, javaFile, "myId");

                //btnShare.Enabled = true;

                if ( controller.SaveToFile(text) )
                {
                    Toast.MakeText(this, GetText(Resource.String.file_location) + " " + fileName, ToastLength.Long).Show();
                    DoShareAudio(fullName);
                }
                else
                {
                    AlertDialog.InfoMessage(this, ":(", GetText(Resource.String.say_error) + '\n' + '\n' + fullName,
                        () =>
                        {
                            if ((int)Build.VERSION.SdkInt >= 23)
                            {
                                const string permission = Manifest.Permission.WriteExternalStorage;
                                var permissionList = new String[] { permission, Manifest.Permission.ReadExternalStorage };

                                if (CheckSelfPermission(permission) != (int)Permission.Granted)
                                    RequestPermissions(permissionList, 0);
                            }
                            return true;
                        }
                    );
                }
            }
            else
            {
                Toast.MakeText(this, GetText(Resource.String.no_text), ToastLength.Long).Show();
            }
        }

        private void DoShareAudio(string audioFileName)
        {
            var localFilePath = audioFileName;
            if (!localFilePath.StartsWith("file://"))
                localFilePath = string.Format("file://{0}", localFilePath);

            var fileUri = Android.Net.Uri.Parse(localFilePath);

            Intent intent = new Intent();
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.SetFlags(ActivityFlags.NewTask);
            intent.SetAction(Intent.ActionSend);
            intent.SetType("*/*");
            intent.PutExtra(Intent.ExtraStream, fileUri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);

            var chooserIntent = Intent.CreateChooser(intent, GetText(Resource.String.share_by));
            chooserIntent.SetFlags(ActivityFlags.ClearTop);
            chooserIntent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(chooserIntent);
        }

    }
}