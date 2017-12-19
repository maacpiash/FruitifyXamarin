using Android.App;
using Android.Widget;
using Android.OS;
using System.IO;
using Android.Graphics;
using Android.Content;
using Android.Provider;
using System.Collections.Generic;
using Android.Content.PM;
using System;
using Java.IO;
using System.Net;
using RestSharp;


namespace Fruitify
{
    [Activity(Label = "Fruitify", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        ImageView imageView;
        Button testButn, button;
        String filePath;
        static string APIURL = "http://40.71.188.243:9999/api/v1_0";
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            button = FindViewById<Button>(Resource.Id.myButton);
            imageView = FindViewById<ImageView>(Resource.Id.imageView1);
            testButn = FindViewById<Button>(Resource.Id.testButn);

            if (ThereIsAnAppToTakePictures())
            {
                CreateDirectoryForPictures();
                button.Click += TakeAPicture;
                testButn.Enabled = App.bitmap != null;
                testButn.Click += SendBitmap;
            }

        }

        private bool ThereIsAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void CreateDirectoryForPictures()
        {
            App._dir = new Java.IO.File(
                Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures), "Fruitify");
            if (!App._dir.Exists())
                App._dir.Mkdirs();
        }

        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            App._file = new Java.IO.File(App._dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));
            intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(App._file));
            StartActivityForResult(intent, 0);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Android.Net.Uri contentUri = Android.Net.Uri.FromFile(App._file);
            filePath = App._file.Path;
            mediaScanIntent.SetData(contentUri);
            SendBroadcast(mediaScanIntent);

            int height = Resources.DisplayMetrics.HeightPixels;
            int width = imageView.Height;
            App.bitmap = App._file.Path.LoadAndResizeBitmap(width, height);
            if (App.bitmap != null)
            {
                imageView.SetImageBitmap(App.bitmap);
                testButn.Enabled = true;
                App.bitmap = null;
            }
            else
                System.Console.WriteLine("Image creation unsuccessful");

            GC.Collect();
        }

        private void SendBitmap(Object sender, EventArgs e)
        {
            var client = new RestClient(APIURL);

            var request = new RestRequest(filePath, Method.PUT);
            var restClient = new RestClient()
            {
                BaseUrl = client.BaseUrl
            };
            //request.AddFile("filedata", filePath, null);

            restClient.ExecuteAsync(request, (response) =>
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    testButn.Text = "Success";
                else
                    testButn.Text = response.ErrorMessage;
            });
        }
    }

    public static class App
    {
        public static Java.IO.File _file;
        public static Java.IO.File _dir;
        public static Bitmap bitmap;
    }

    public static class BitmapHelpers
    {
        public static Bitmap LoadAndResizeBitmap(this string fileName, int width, int height)
        {
            BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(fileName, options);
            int outHeight = options.OutHeight;
            int outWidth = options.OutWidth;
            int inSampleSize = 1;

            if (outHeight > height || outWidth > width)
                inSampleSize = outWidth > outHeight ? outHeight / height : outWidth / width;
            
            options.InSampleSize = inSampleSize;
            options.InJustDecodeBounds = false;
            Bitmap resizedBitmap = BitmapFactory.DecodeFile(fileName, options);

            return resizedBitmap;
        }
    }
}

