using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LockScreenApp.Resources;
using Microsoft.Phone.Scheduler;
using Windows.Phone.System.UserProfile;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.IO;
namespace LockScreenApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        ObservableCollection<ImgList> imglists;
        List<string> actpho = new List<string>();
        public MainPage()
        {
            InitializeComponent();
            imglists = new ObservableCollection<ImgList>();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (!settings.Contains("photolen"))
            {
                settings.Add("photolen", "0");
            }
            if (!settings.Contains("navchk"))
            {
                settings.Add("navchk", "0");
            }
            else
            {
                if(settings["navchk"] as string != "0")
                {
                    LoadActivePhotos();
                }
            }
            
            imglist.ItemsSource = imglists;
            //GetLockScreenPermission();
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

        }
        public async void GetLockScreenPermission()
        {
            if (!LockScreenManager.IsProvidedByCurrentApplication)
            {
                var result = await LockScreenManager.RequestAccessAsync();
                if (result == LockScreenRequestResult.Granted)
                {
                    MessageBox.Show("Now You Can Change Your LockScreen for every 20 minutes.");
                    MessageBox.Show("Choose the photos from your library by clicking Add Photos button");
                    MessageBox.Show("Click the Change Lockscreen button to change it for every 20 minutes");
                }
            }
        }


        public async void SaveActivePhotos(List<string> actlist)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            if (actlist.Count > 0)
            {
                string save = "";
                foreach (string s in actlist)
                {
                    save += s + ",";
                }
                settings["navchk"] = save;
                settings.Save();
            }
            else
            {
                settings["navchk"] = "0";
                settings.Save();
            }
        }


        public async void LoadActivePhotos() 
        {
            string[] stringSeparators = new string[] { "," };
            string[] result;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            var ap = settings["navchk"] as string;
            ap = ap.EndsWith(",") ? ap.Substring(0, ap.Length - 1) : ap;
            result = ap.Split(stringSeparators, StringSplitOptions.None);
            foreach(string s in result)
            {
                actpho.Add(s);
                IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(s+".jpg", System.IO.FileMode.Open, file))
                {
                    BitmapImage image = new BitmapImage();
                    image.SetSource(stream);
                    imglists.Add(new ImgList() { ImgSrc = image, IS = s, Photos = stream });
                }
            }

        }

        public async void StartLockScreenImageChange()
        {
             var photolen = IsolatedStorageSettings.ApplicationSettings["navchk"] as string;
             if (photolen != "0")
             {
                 var taskName = "my task";
                 if (ScheduledActionService.Find(taskName) != null)
                 {
                     ScheduledActionService.Remove(taskName);
                 }
                 var periodicTask = new PeriodicTask(taskName) { Description = "some desc" };
                 try
                 {
                     ScheduledActionService.Add(periodicTask);
                     ScheduledActionService.LaunchForTest(taskName, TimeSpan.FromSeconds(10));
                 }
                 catch
                 {
                     //handle
                 }
             }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!LockScreenManager.IsProvidedByCurrentApplication)
            {
                var result = await LockScreenManager.RequestAccessAsync();
                if (result == LockScreenRequestResult.Granted)
                {
                    var photolen = IsolatedStorageSettings.ApplicationSettings["navchk"] as string;
                    if (photolen == "0")
                    {
                        MessageBox.Show("Please choose some images");
                    }
                    else
                    {
                        StartLockScreenImageChange();
                    }
                }
            }
            else
            {
                var photolen = IsolatedStorageSettings.ApplicationSettings["navchk"] as string;
                if (photolen == "0")
                {
                    MessageBox.Show("Please choose some images");
                }
                else
                {
                    StartLockScreenImageChange();
                }
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            PhotoChooserTask choose = new PhotoChooserTask();
            choose.Completed += choose_Completed;
            choose.Show();
        }

        void choose_Completed(object sender, PhotoResult e)
        {
            if (e.ChosenPhoto != null)
            {
                string photolen = "";
                ImgList immm = new ImgList();
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                photolen = IsolatedStorageSettings.ApplicationSettings["photolen"] as string;
                int pl = int.Parse(photolen);
                pl = pl + 1;
                immm.IS = pl.ToString();
                immm.Photos = e.ChosenPhoto;
                IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication();
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(pl.ToString() + ".jpg", System.IO.FileMode.Create, file))
                {
                    byte[] buffer = new byte[1024];
                    while (e.ChosenPhoto.Read(buffer, 0, buffer.Length) > 0)
                    {
                        stream.Write(buffer, 0, buffer.Length);
                        settings["photolen"] = pl.ToString();
                        settings.Save();
                    }
                    BitmapImage image = new BitmapImage();
                    image.SetSource(stream);
                    immm.ImgSrc = image;
                    imglists.Add(immm);
                    actpho.Add(immm.IS);
                    SaveActivePhotos(actpho);
                }
               
            }
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            PhotoChooserTask choose = new PhotoChooserTask();
            choose.Completed += choose_Completed;
            choose.Show();
        }

        private void LongListMultiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void ApplicationBarMenuItem_Click_1(object sender, EventArgs e)
        {
            if(imglist.SelectedItems.Count > 0)
            {
                if (LockScreenManager.IsProvidedByCurrentApplication)
                {
                    LockScreen.SetImageUri(
                           new Uri("ms-appx:///mankatha.png",
                            UriKind.Absolute));
                }
                var taskName = "my task";
                if (ScheduledActionService.Find(taskName) != null)
                {
                    ScheduledActionService.Remove(taskName);
                }
                ObservableCollection<ImgList> delcol = new ObservableCollection<ImgList>();
                foreach(ImgList toberemoved in imglist.SelectedItems)
                {
                    delcol.Add(toberemoved);
                    actpho.Remove(toberemoved.IS);
                }
                foreach(ImgList rem in delcol)
                {
                    imglists.Remove(rem);
                }
                SaveActivePhotos(actpho);
                if (LockScreenManager.IsProvidedByCurrentApplication)
                {
                    StartLockScreenImageChange();
                }
                
            }
            
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
    class ImgList : INotifyPropertyChanged
    {
        private BitmapImage imgSrc;
        private string is1;
        private Stream photos;
        public BitmapImage ImgSrc
        {
            get { return imgSrc; }
            set { imgSrc = value; OnPropertyChanged("ImgSrc"); }
        }
        public Stream Photos
        {
            get { return photos; }
            set { photos = value; OnPropertyChanged("Photos"); }
        }
        
        public string IS
        {
            get { return is1; }
            set { is1 = value; OnPropertyChanged("IS"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}