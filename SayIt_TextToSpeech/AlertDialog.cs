using Android.Content;
using System;

namespace SayIt_TextToSpeech
{
    class AlertDialog
    {
        static public void InfoMessage(Context context, string caption, string message, Func<bool> onOkClick)
        {
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(context);
            Android.App.AlertDialog alert = dialog.Create();
            alert.SetTitle(caption);
            alert.SetMessage(message);
            alert.SetButton("OK", (c, ev) =>
            {
                onOkClick?.Invoke();
                return;
            });
            alert.Show();
        }
    }
}