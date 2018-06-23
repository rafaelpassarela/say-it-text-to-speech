using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;

namespace SayIt_TextToSpeech
{
    class UtteranceProgressListenerWrapper : UtteranceProgressListener
    {

        private readonly Func<string, bool> _onStart;
        private readonly Func<string, bool> _onDone;
        private readonly Func<string, bool> _onError;

        public UtteranceProgressListenerWrapper(
            Func<string, bool> onDone, Func<string, bool> onError, Func<string, bool> onStart)
        {
            _onDone = onDone;
            _onError = onError;
            _onStart = onStart;
        }

        public override void OnDone(string utteranceId)
        {
            _onDone(utteranceId);
        }

        public override void OnError(string utteranceId)
        {
            _onError(utteranceId);
        }

        public override void OnStart(string utteranceId)
        {
            _onStart(utteranceId);
        }
    }
}