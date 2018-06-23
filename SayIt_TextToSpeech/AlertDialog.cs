using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SayIt_TextToSpeech
{
    class AlertDialog
    {        
        static public void InfoMessage(Context context, string caption, string message)
        {
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(context);
            Android.App.AlertDialog alert = dialog.Create();
            alert.SetTitle(caption);
            alert.SetMessage(message);
            alert.SetButton("OK", (c, ev) =>
            {
                return;
            });
            alert.Show();
        }
    }
}