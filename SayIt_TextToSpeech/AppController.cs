using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Java.Util;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Speech.Tts;

namespace SayIt_TextToSpeech
{
    class AppController
    {
        private List<string> _langList = new List<string>();
        private TextToSpeech _textToSpeech;
        private readonly Context _context;

        public Locale SelectedLocale { get; set; }
        public List<string> LangAvailable => _langList;

        public AppController(Context context, TextToSpeech.IOnInitListener initListener, UtteranceProgressListenerWrapper listenerWrapper)
        {
            _context = context;

            SetDefaultLocale();

            // set up the TextToSpeech object
            // third parameter is the speech engine to use
            _textToSpeech = new TextToSpeech(context, initListener, "com.google.android.tts");

            // set up the speech to use the default langauge
            // if a language is not available, then the default language is used.
            _textToSpeech.SetLanguage(SelectedLocale);

            // set the speed and pitch
            _textToSpeech.SetPitch(1f);
            _textToSpeech.SetSpeechRate(1f);

            _textToSpeech.SetOnUtteranceProgressListener(listenerWrapper);
        }

        public void UpdateLocale(int id)
        {
            SelectedLocale = Locale.GetAvailableLocales().FirstOrDefault(
                    t => t.DisplayName == LangAvailable[id]);
        }

        public void UpdateTTSLanguage()
        {
            _textToSpeech.SetLanguage(SelectedLocale);
        }

        public void SetDefaultLocale()
        {
            // set up the speech to use the default langauge            
            SelectedLocale = Locale.Default;
        }

        public void SetPitch(float pitch)
        {
            _textToSpeech.SetPitch(pitch);
        }

        public void SetSpeechRate(float speechRate)
        {
            _textToSpeech.SetSpeechRate(speechRate);
        }

        public void InitializeLocaleList()
        {
            // referencing to the local var, the LangAvailable property can't be replaced, it's read-only
            _langList.Clear();
            _langList.Add("Default");

            // our spinner only wants to contain the languages supported by the tts and ignore the rest
            var localesAvailable = Locale.GetAvailableLocales().ToList();
            foreach (var locale in localesAvailable)
            {
                LanguageAvailableResult res = _textToSpeech.IsLanguageAvailable(locale);

                if (IsLocaleAvailable(locale))
                {
                    _langList.Add(locale.DisplayName);
                }
            }
            _langList = _langList.OrderBy(t => t).Distinct().ToList();
        }

        public bool IsLocaleAvailable()
        {
            return IsLocaleAvailable(SelectedLocale);
        }

        public bool IsLocaleAvailable(Locale testLocale)
        {
            if (testLocale == null)
                return false;

            if (Locale.Default.DisplayName == testLocale.DisplayName)
                return true;

            LanguageAvailableResult res = _textToSpeech.IsLanguageAvailable(testLocale);

            return (res == LanguageAvailableResult.Available)
                || (res == LanguageAvailableResult.CountryAvailable)
                || (res == LanguageAvailableResult.CountryVarAvailable);
        }

        public bool Speak(string text)
        {
            try
            {
                _textToSpeech.Speak(text, QueueMode.Flush, null);

                return true;
            }
            catch (Exception e)
            {
                Toast.MakeText(_context, e.Message, ToastLength.Long).Show();
                return false;
            }
        }

        public bool SaveToFile(string text)
        {
            var path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var fileName = _context.GetText(Resource.String.say_button) + "_" + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".mp3";
            var fullName = Path.Combine(path.AbsolutePath, fileName);

            Java.IO.File javaFile = new Java.IO.File(fullName);
            var res = _textToSpeech.SynthesizeToFile(text, null, javaFile, "myId");

            return res == OperationResult.Success;
        }
    }
}