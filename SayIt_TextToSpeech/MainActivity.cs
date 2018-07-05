using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using System.Threading;

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
        private bool saveDone = false;

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
                    saveDone = true;
                    return true;
                },
                (string x) =>
                {
                    //onError
                    saveDone = false;
                    AlertDialog.InfoMessage(this, ":(", GetText(Resource.String.say_error), null);
                    return false;
                },
                (string x) =>
                {
                    //onBegin
                    saveDone = false;
                    return true;
                }
            );

            controller = new AppController(this, this, this, listner);

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
                saveDone = false;
            };

            btnClear.Click += delegate
            {
                editWhatToSay.Text = "";
                saveDone = false;
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

        private void SaveAudio(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                saveDone = false;
                if ( controller.SaveToFile(text) )
                {
                    Toast.MakeText(this, GetText(Resource.String.file_location) + " " + controller.OutputFile.FileName, ToastLength.Long).Show();
                    DoShareAudio();
                }
            }
            else
            {
                Toast.MakeText(this, GetText(Resource.String.no_text), ToastLength.Long).Show();
            }
        }

        private void DoShareAudio()
        {
            int attemptCount = 0;
            // wait the callback funtion from  UtteranceProgressListener
            // saveDone becone false when starts the SaveAudio method and become true on callback
            while (!saveDone || (attemptCount > 90))
            {
                Thread.Sleep(100);
                // every millisecond has 9 attempts (in this case), so we will wait for ten secs.
                // 9 * 10 = 90 attemps at total, if attemptCount > 90 it's time to move on
                attemptCount++;
            }

            controller.ShareFile();
            saveDone = false;
        }

    }
}