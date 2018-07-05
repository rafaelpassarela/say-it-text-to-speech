using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Speech.Tts;
using Android.Widget;
using Java.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SayIt_TextToSpeech
{
    class AppController
    {
        private List<string> _langList = new List<string>();
        private readonly TextToSpeech _textToSpeech;
        private readonly Context _context;
        private readonly Activity _activity;

        public Locale SelectedLocale { get; set; }
        public List<string> LangAvailable => _langList;
        public OutputFileModel OutputFile { get; }
        
        public AppController(Context context, Activity activity, TextToSpeech.IOnInitListener initListener, UtteranceProgressListenerWrapper listenerWrapper)
        {
            _context = context;
            _activity = activity;

            OutputFile = new OutputFileModel(_context.GetText(Resource.String.save_dir));

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
            OutputFile.FileName = _context.GetText(Resource.String.say_button) + "_" + DateTime.Now.ToString("yyyy_mm_dd_HH_mm_ss") + ".wav";

            var name = OutputFile.FilePath;
            if (!Directory.Exists(name))
            {
                try
                {
                    Directory.CreateDirectory(name);
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(UnauthorizedAccessException))
                    {
                        AlertDialog.InfoMessage(
                            _context,
                            _context.GetText(Resource.String.no_dir_caption),
                            _context.GetText(Resource.String.no_dir_text) + "\n\n" + name,
                            () => RequestPermission());
                        return false;
                    }
                    throw;
                }
            }

            Java.IO.File javaFile = new Java.IO.File(OutputFile.FileFullName);
            var res = _textToSpeech.SynthesizeToFile(text, null, javaFile, "myId");

            if (res != OperationResult.Success)
            {
                AlertDialog.InfoMessage(_context, ":(", _context.GetText(Resource.String.say_error) + '\n' + '\n' + OutputFile.FileFullName,
                    () => RequestPermission());
                return false;
            }
            OutputFile.LastTextConverted = text;

            return true;
        }

        public bool RequestPermission()
        {
            if ((int)Build.VERSION.SdkInt >= 23)
            {
                const string permission = Manifest.Permission.WriteExternalStorage;
                var permissionList = new String[] { permission, Manifest.Permission.ReadExternalStorage };

                if (_context.CheckSelfPermission(permission) != (int)Permission.Granted)
                    _activity.RequestPermissions(permissionList, 0);

                return true;
            }
            return false;
        }

        public bool ShareFile(bool shareText)
        {
            var localFilePath = OutputFile.FileFullName;

            if (!localFilePath.StartsWith("file://"))
                localFilePath = string.Format("file://{0}", localFilePath);

            var fileUri = Android.Net.Uri.Parse(localFilePath);

            Intent intent = new Intent();
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.SetFlags(ActivityFlags.NewTask);
            intent.SetAction(Intent.ActionSend);
            intent.SetType("*/*");

            // add the text to the share intent
            if (shareText)
            {
                intent.PutExtra(Intent.ExtraText, OutputFile.LastTextConverted);
            }
            // add the audio stream to the share intent
            intent.PutExtra(Intent.ExtraStream, fileUri);
            
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);

            var chooserIntent = Intent.CreateChooser(intent, _context.GetText(Resource.String.share_by));
            chooserIntent.SetFlags(ActivityFlags.ClearTop);
            chooserIntent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(chooserIntent);

            return true;
        }
    }
}