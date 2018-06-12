using Android.App;
using Android.Content;
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

//ctrl r + g
//ctrl k + d

namespace SayIt_TextToSpeech
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity,
        TextToSpeech.IOnInitListener
    {
        private TextToSpeech textToSpeech;
        private Locale selectedLocale;
        private readonly int needLang = 103;
        private readonly int needConfig = 203;
        private Spinner spinLanguages;
        private Button btnShare;
        private List<string> langAvailable = new List<string>();
        Context context;

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            // if get error, set default language
            if (status == OperationResult.Error)
                textToSpeech.SetLanguage(Locale.Default);
            // listener ok, set lang
            if (status == OperationResult.Success)
            {
                langAvailable.Clear();
                langAvailable.Add("Default");
                // our spinner only wants to contain the languages supported by the tts and ignore the rest
                var localesAvailable = Locale.GetAvailableLocales().ToList();
                foreach (var locale in localesAvailable)
                {
                    LanguageAvailableResult res = textToSpeech.IsLanguageAvailable(locale);

                    if (IsLocaleAvailable(locale))
                    {
                        langAvailable.Add(locale.DisplayName);
                    }
                }
                langAvailable = langAvailable.OrderBy(t => t).Distinct().ToList();

                var adapter = new ArrayAdapter<string>(
                    this, Android.Resource.Layout.SimpleSpinnerDropDownItem, langAvailable);
                spinLanguages.Adapter = adapter;
                spinLanguages.SetSelection(langAvailable.IndexOf(selectedLocale.DisplayName));
                textToSpeech.SetLanguage(selectedLocale);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            // base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == needConfig || (requestCode == needLang && !IsLocaleAvailable(selectedLocale)))
            {
                // we need a new language installed
                var installTTS = new Intent();
                installTTS.SetAction(TextToSpeech.Engine.ActionInstallTtsData);
                StartActivity(installTTS);
            }
            else if (IsLocaleAvailable(selectedLocale))
            {
                textToSpeech.SetLanguage(selectedLocale);
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

            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var editWhatToSay = FindViewById<EditText>(Resource.Id.editSpeech);
            var txtSpeedVal = FindViewById<TextView>(Resource.Id.textSpeed);
            var txtPitchVal = FindViewById<TextView>(Resource.Id.textPitch);
            var seekSpeed = FindViewById<SeekBar>(Resource.Id.seekSpeed);
            var seekPitch = FindViewById<SeekBar>(Resource.Id.seekPitch);
            var btnClear = FindViewById<Button>(Resource.Id.btnClear);
            btnShare = FindViewById<Button>(Resource.Id.btnShare);
            spinLanguages = FindViewById<Spinner>(Resource.Id.spinLanguage);

            // set up the initial pitch and speed values then the onscreen values
            // the pitch and rate both go from 0f to 1f, however if you have a seek bar with a max of 1, you get a single step
            // therefore, a simpler option is to have the slider go from 0 to 255 and divide the position of the slider by 255 to get
            // the float
            seekPitch.Progress = 254;
            seekSpeed.Progress = 254;
            txtPitchVal.Text = "1.00";
            txtSpeedVal.Text = "1.00";

            // get the context - easiest way is to obtain it from an on screen gadget
            context = btnSayIt.Context;

            // set up the TextToSpeech object
            // third parameter is the speech engine to use
            textToSpeech = new TextToSpeech(this, this, "com.google.android.tts");
            //textToSpeech.SetOnUtteranceCompletedListener(this);

            // set up the speech to use the default langauge
            // if a language is not available, then the default language is used.
            selectedLocale = Locale.Default;
            textToSpeech.SetLanguage(selectedLocale);

            // set the speed and pitch
            textToSpeech.SetPitch(1f);
            textToSpeech.SetSpeechRate(1f);

            // connect up the events
            btnSayIt.Click += delegate
            {
                // if there is nothing to say, don't say it
                if (!string.IsNullOrEmpty(editWhatToSay.Text))
                    textToSpeech.Speak(editWhatToSay.Text, QueueMode.Flush, null);
                else
                    Toast.MakeText(this, GetText(Resource.String.no_text), ToastLength.Long).Show();
            };

            btnClear.Click += delegate
            {
                editWhatToSay.Text = "";
            };

            btnShare.Click += delegate
            {
                ShareTranslatedAudio(editWhatToSay.Text);
            };

            // sliders
            seekPitch.StopTrackingTouch += (object sender, SeekBar.StopTrackingTouchEventArgs e) =>
            {
                var seek = sender as SeekBar;
                var progress = seek.Progress / 255f;
                textToSpeech.SetPitch(progress);
                txtPitchVal.Text = progress.ToString("F2");
            };

            seekSpeed.StopTrackingTouch += (object sender, SeekBar.StopTrackingTouchEventArgs e) =>
            {
                var seek = sender as SeekBar;
                var progress = seek.Progress / 255f;
                textToSpeech.SetSpeechRate(progress);
                txtSpeedVal.Text = progress.ToString("F2");
            };

            spinLanguages.ItemSelected += (object sender, AdapterView.ItemSelectedEventArgs e) =>
            {
                // find the selected locale
                selectedLocale = Locale.GetAvailableLocales().FirstOrDefault(t => t.DisplayName == langAvailable[(int)e.Id]);
                // create intent to check the TTS has this language installed
                var checkTTSIntent = new Intent();
                checkTTSIntent.SetAction(TextToSpeech.Engine.ActionCheckTtsData);
                StartActivityForResult(checkTTSIntent, needLang);
            };
        }

        private void ShareTranslatedAudio(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                btnShare.Enabled = false;

                var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                var fileName = GetText(Resource.String.say_button) + "_" + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".mp3";
                var fullName = Path.Combine(path.AbsolutePath, fileName);

                Java.Lang.ICharSequence chars = new Java.Lang.String(text);
                Java.IO.File javaFile = new Java.IO.File(fullName);
                var res = textToSpeech.SynthesizeToFile(chars, null, javaFile, "myId");

                btnShare.Enabled = true;
                if (res == OperationResult.Success)
                {
                    Toast.MakeText(this, GetText(Resource.String.file_location) + fileName, ToastLength.Long).Show();
                    DoShareAudio(fullName);
                }
                else
                {
                    Toast.MakeText(this, GetText(Resource.String.save_error) + fileName, ToastLength.Long).Show();
                }
            }
            else
            {
                Toast.MakeText(this, GetText(Resource.String.no_text), ToastLength.Long).Show();
            }
        }

        private void DoShareAudio(string audioFileName)
        {
            //var f = new File(audioFileName);
            //Uri uri = Uri.parse("file://" + f.getAbsolutePath());

            //Java.IO.File file = new Java.IO.File(audioFileName);
            //var uri = FileProvider.GetUriForFile(this, BuildConfig.ApplicationId + ".provider", file);
            
            // send as audio stream
            Intent intentSend = new Intent();
            intentSend.SetAction(Intent.ActionSend);
            intentSend.SetType("audio/mp3");
            //intentSend.PutExtra(Intent.ExtraStream, uri);
            //intentSend.PutExtra(Intent.ExtraStream, "file:///" + audioFileName);
            intentSend.AddFlags(ActivityFlags.GrantReadUriPermission);
            
            StartActivity(Intent.CreateChooser(intentSend, GetText(Resource.String.share_by)));
        }

        private bool IsLocaleAvailable(Locale testLocale)
        {
            if (testLocale == null)
                return false;

            if (Locale.Default.DisplayName == testLocale.DisplayName)
                return true;

            LanguageAvailableResult res = textToSpeech.IsLanguageAvailable(testLocale);

            return (res == LanguageAvailableResult.Available)
                || (res == LanguageAvailableResult.CountryAvailable)
                || (res == LanguageAvailableResult.CountryVarAvailable);
        }
    }
}