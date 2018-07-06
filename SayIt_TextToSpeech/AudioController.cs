using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SayIt_TextToSpeech
{
    class AudioController
    {
        private readonly Context _context;

        public AudioController(Context context)
        {
            _context = context;
        }

        public bool MergeSilent(string audioFile)
        {
            // load the silet file from our Assets
            Stream asset = _context.Assets.Open("Silent_1seg.wav");

            // load the current audio file to a stream
            MemoryStream audio = new MemoryStream();
            using (FileStream fs = File.OpenRead(audioFile))
            {
                fs.CopyTo(audio);
            }
            // copy to the end of stream
            audio.Position = audio.Length;
            asset.CopyTo(audio);

            audio.Position = 0;
            var tmp = audioFile + "_m.wav";
            using (FileStream ws = File.OpenWrite(tmp))
            {
                audio.WriteTo(ws);
                ws.Close();
            }

            return true;
        }
    }
}