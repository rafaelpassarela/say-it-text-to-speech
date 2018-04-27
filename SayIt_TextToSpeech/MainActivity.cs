using System;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Speech.Tts;
using System.Collections.Generic;
using System.Linq;
using Android.Runtime;
using Java.Util;

namespace SayIt_TextToSpeech
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity, TextToSpeech.IOnInitListener
    {
        private TextToSpeech textToSpeech;
        private Locale selectedLocale;
        private readonly int needLang = 103;
        Context context;

        public void OnInit([GeneratedEnum] OperationResult status)
        {
            // if get error, set default language
            if (status == OperationResult.Error)
                textToSpeech.SetLanguage(Locale.Default);
            // listener ok, set lang
            if (status == OperationResult.Success)
                textToSpeech.SetLanguage(selectedLocale);

// fix not supported locale
//https://stackoverflow.com/questions/47062498/android-google-tts-why-langavailable-returns-not-supported-or-2

        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            // base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == needLang && !IsLocaleAvailable(selectedLocale))
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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var btnSayIt = FindViewById<Button>(Resource.Id.btnSpeak);
            var editWhatToSay = FindViewById<EditText>(Resource.Id.editSpeech);
            var spinLanguages = FindViewById<Spinner>(Resource.Id.spinLanguage);
            var txtSpeedVal = FindViewById<TextView>(Resource.Id.textSpeed);
            var txtPitchVal = FindViewById<TextView>(Resource.Id.textPitch);
            var seekSpeed = FindViewById<SeekBar>(Resource.Id.seekSpeed);
            var seekPitch = FindViewById<SeekBar>(Resource.Id.seekPitch);

            // set up the initial pitch and speed values then the onscreen values
            // the pitch and rate both go from 0f to 1f, however if you have a seek bar with a max of 1, you get a single step
            // therefore, a simpler option is to have the slider go from 0 to 255 and divide the position of the slider by 255 to get
            // the float
            seekSpeed.Progress = seekPitch.Progress = 127;
            txtSpeedVal.Text = txtPitchVal.Text = "0.5";

            // get the context - easiest way is to obtain it from an on screen gadget
            context = btnSayIt.Context;

            // set up the TextToSpeech object
            // third parameter is the speech engine to use
            textToSpeech = new TextToSpeech(this, this, "com.google.android.tts");

            // set up the langauge spinner
            // set the top option to be default
            var langAvailable = new List<string> { "Default" };

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

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, langAvailable);
            spinLanguages.Adapter = adapter;

            // set up the speech to use the default langauge
            // if a language is not available, then the default language is used.
            selectedLocale = Locale.Default;
            textToSpeech.SetLanguage(selectedLocale);

            // set the speed and pitch
            textToSpeech.SetPitch(.5f);
            textToSpeech.SetSpeechRate(.5f);

            // connect up the events
            btnSayIt.Click += delegate
            {
                // if there is nothing to say, don't say it
                if (!string.IsNullOrEmpty(editWhatToSay.Text))
                    textToSpeech.Speak(editWhatToSay.Text, QueueMode.Flush, null);
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

        private bool IsLocaleAvailable(Locale testLocale)
        {
            if (testLocale = null)
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