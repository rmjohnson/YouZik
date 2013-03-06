using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;
using System.Windows.Automation.Peers;
using System.Runtime.InteropServices;
using System.Threading;
//using Utilities.Web.WebBrowserHelper;
using CefSharp;
using System.Windows.Automation.Provider;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace YouZik
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        List<String> videoIDs; //List of youtube video IDs, used to retrieve videos from youtube
        ObservableCollection<Artist> artists; //Artist names list
        CefSharp.Wpf.WebView YouTubePlayer; //The webview used to display the youtube player
        public ObservableCollection<Song> songs = new ObservableCollection<Song>();
        Artist currentArtist;

        public MainWindow()
        {
            InitializeComponent(); 
            selectText(); //Start out with textbox text selected
            YouTubePlayer = new CefSharp.Wpf.WebView("", new BrowserSettings()); //Create the youtube player web view
            //Register the scripthelper as a JS object so javascript can access it's methods
            YouTubePlayer.RegisterJsObject("scripthelper", new ScriptHelper(this));
            BrowserGrid.Children.Add(YouTubePlayer); //Add the youtube player web view to its grid in the main window
            artists = new ObservableCollection<Artist>(); //Initialize the artists list
            SongList.ItemsSource = songs;
            ArtistList.ItemsSource = artists;
        }

        public void selectText()
        {
            QueryBox.Focus(); //Make the querybox the focus
            QueryBox.SelectionStart = 0; //Start at the beginning of the text
            QueryBox.SelectionLength = QueryBox.Text.Length; //Set the selection length to the length of the entire text
        }

        //The search button's method
        public void searchButton(object sender, RoutedEventArgs e)
        {
            search(); //Run the search method
        }

        //The search method
        public void search()
        {
            Song tmpSong;
            //Clear all the previous songs in the SongList
            //SongList.Items.Clear();

            //Set the application name and API key and create the request
            YouTubeRequestSettings settings = new YouTubeRequestSettings("YouZik", "AI39si6y5lkNycte-kyM_2MiPgXYInjosqAhZomQEZvoibjxlweo0Nvk0vCLmN0Z0JX4f5QZavKOHrRNvYYQ04YdhyEqGsow7g");
            YouTubeRequest request = new YouTubeRequest(settings);

            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri); //Create the query
            query.Query = QueryBox.Text + " lyrics"; //Add the querybox's text to the query plus the word "lyrics"
            query.ExtraParameters = "format=5"; //Add more parameters to increase results and to attempt to filter out non-embeddable videos

            try //Try, used to catch if the query failed (such as if the user doesn't have an active internet connection)
            {
                Feed<Video> videoFeed = request.Get<Video>(query); //Send the query and insert the results into videoFeed
                videoIDs = new List<String>(); //Initialize the videoIDs list to an empty string list
                currentArtist = new Artist(QueryBox.Text);
                foreach (Video entry in videoFeed.Entries) //Loop through each entry in the results
                {
                    //If the state is null is used because some videos have their embedded restrictions stored in the state
                    if (entry.YouTubeEntry.State == null)
                    {
                        videoIDs.Add(entry.VideoId); //Add the video ID to the video IDs list
                        tmpSong = new Song(currentArtist.artistID, entry.VideoId, entry.Title + " - " + toTimeCode(entry.Media.Duration.Seconds));
                        if (artists.Count == 1) //If it is the first artist, then the songs need to be added.
                        {
                            songs.Add(tmpSong);
                        }
                        else
                        {
                            songs.Insert(videoIDs.Count-1, tmpSong); //Add the title and the duration to the song list
                        }
                    }
                }
                if (videoIDs.Count == 0) //If there were no videos added the video IDs list
                {
                    MessageBox.Show("No videos were found. =("); //Display a message that no songs were found
                }
                else //Otherwise...
                {
                    SongList.SelectedIndex = 0; //Select the first song in the song list (it will automatically be played because of the newSong method)
                }
            }
            catch (Google.GData.Client.GDataRequestException e) //Catch the bad request exception
            {
                MessageBox.Show("Please check to make sure you have an internet connection."); //Display an error message
            }

        }

        //newSong method that is triggered whenever a different song is selected in the song list, whether by the user or the program
        public void newSong(object sender, RoutedEventArgs e)
        {
            if (SongList.SelectedIndex != -1) //If a song on the list is indeed selected (the selected index could change to nothing, aka -1 index)
            {
                play(); //Play the song
            }
        }

        //When the next song button is pressed
        public void nextSongButton(object sender, RoutedEventArgs e)
        {
            nextSong(); //Run the next song method
        }

        public void nextSong()
        {
            if (SongList.SelectedIndex < videoIDs.Count-1) //If the song isn't the last song in the list
            {
                SongList.SelectedIndex++; //Increment the song list selected index
            }
            else //Otherwise...
            {
                SongList.SelectedIndex = 0; //Select the first song (loop back around)
            }
        }

        //When the previous song button is pressed
        public void prevSongButton(object sender, RoutedEventArgs e)
        {
            prevSong(); //Run the previous song method
        }
        public void prevSong()
        {
            if (SongList.SelectedIndex > 0) //If the selected song isn't the first one
            {
                SongList.SelectedIndex--; //Decrement the song list selected index
            }
        }

        public void play()
        {
            YouTubePlayer.LoadHtml(generateHTML()); //Load the generated HTML into the web view
        }

        //Generate the necessary HTML so that the player API can be used to detect when a song is finished or when a song has an error
        public String generateHTML() 
        {
            String html =
                @"<html><body><div style=""text-align:center""><div id=""player""></div></div><script type=""text/Javascript"">
                  var tag = document.createElement('script');
                  tag.src = ""http://www.youtube.com/iframe_api"";
                  var firstScriptTag = document.getElementsByTagName('script')[0];
                  firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
                  var player;
                  function onYouTubeIframeAPIReady() {
                    player = new YT.Player('player', {
                    width: 320,
                    height: 240,
                    playerVars: {'autoplay': 1},
                    videoId: '" + songs[SongList.SelectedIndex].videoID + @"',
                    events: {
                        'onReady': onPlayerReady,
                        'onStateChange': onPlayerStateChange,
                        'onError': onPlayerError
                    }
                   });
                  }
                  function onPlayerReady(event) {
                    event.target.playVideo();
                  }
                  function onPlayerStateChange(event) {
                    scripthelper.setStatus(event.data);
                    if (event.data == YT.PlayerState.ENDED) {
                        scripthelper.nextSong();
                    }
                  }
                  function onPlayerError(event) {
                    if (event.data == 101 || event.data == 150) {
                        scripthelper.deleteSong();
                    }
                  }  
                  </script></body></html> 
            ";
            return html;
        }

        //Converts the given duration (in seconds) to minutes and seconds
        public String toTimeCode(String seconds)
        {
            return Convert.ToString(Convert.ToInt32(seconds) / 60) + ":" + Convert.ToString(Convert.ToInt32(seconds) % 60).PadLeft(2,'0');
        }

        //When a key is pressed on the query box
        private void enterHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) //If the key is the enter / return key
            {
                search(); //Run the search query
            }
        }

        private void addArtistButton(object sender, RoutedEventArgs e)
        {
            addArtist();  
        }
        private void addArtist()
        {
            artists.Add(currentArtist);
            QueryBox.Clear();
        }

        private void deleteArtistButton(object sender, RoutedEventArgs e)
        {
            deleteArtist();
        }
        private void deleteArtist()
        {
            List<int> removeList = new List<int>();
            //Loop through the songs and find any that are by the selected artist
            foreach (Song song in songs)
            {
                if (song.artistID == artists[ArtistList.SelectedIndex].artistID)
                    removeList.Add(songs.IndexOf(song));
            }
            //Remove all of the ones that have been found (because removing in the original foreach loop is a no-no)
            //and start from the top of the list so the necessary indexes aren't moved once it is removed
            removeList.Reverse(0,removeList.Count);
            foreach (int index in removeList)
            {
                songs.RemoveAt(index);
            }
            artists.RemoveAt(ArtistList.SelectedIndex);
        }

        //Triggered by the script handler
        public void deleteSong()
        {
            Status.Content = "Finding next playable song..."; //Set the status message so the user knows what is going on
            nextSong(); //Play the next song
            songs.RemoveAt(SongList.SelectedIndex-1); //Remove the previous song from the song list
            //Remove the song from the artist videos list
        }

        private void shuffleButton(object sender, RoutedEventArgs e)
        {
            shuffle();
        }

        public void shuffle()
        {
            var tmpSongs = new ObservableCollection<Song>();
            bool[] randomized = new bool[songs.Count];
            Random random = new Random();
            int randomIndex;
            while (randomized.Contains(false))
            {
                randomIndex = random.Next(0, songs.Count);
                if (!randomized[randomIndex])
                {
                    tmpSongs.Add(songs[randomIndex]);
                    randomized[randomIndex] = true;
                }
            }
            songs.Clear();
            foreach (Song song in tmpSongs)
            {
                songs.Add(song);
            }
            SongList.SelectedIndex = 0;
        }

        //Triggered by the script handler
        public void setStatus(String status)
        {
            if(status != null) //If the status isn't null, update the status
                Status.Content = status;
        }

        [ComVisible(true)]
        public class ScriptHelper //The scripthelper, used to communicate between the UI and the javascript on the web view
        {
            MainWindow window;
            public ScriptHelper(MainWindow window) //Construct the scripthelper with the mainwindow so that its methods can be called
            {
                this.window = window;
            }
            public void nextSong() //Triggered by the javascript API detecting the end of a song
            {
                //Send the window.nextSong() method to the dispatcher that will transfer it across threads
                window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                delegate()
                {
                    window.nextSong();
                }));
            }
            public void deleteSong() //Triggered by the javascript API detecting an error with the current song
            {
                //Send the window.deleteSong() method to the dispatcher that will transfer it across threads
                window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                delegate()
                {
                    window.deleteSong();
                }));
            }
            public void setStatus(int statusCode) //Triggered by the javascript API detecting a change in the status of the video
            {
                String status; //Declate the status string
                switch (statusCode) //Switch through the given status code
                {
                    case 1: //If the song is currently playing
                        status = "Playing";
                        break;
                    case 2: //If the song is paused
                        status = "Paused";
                        break;
                    case 3: //If the song is buffering
                        status = "Buffering";
                        break;
                    default: //If something else unexpected is happening
                        status = null;
                        break;
                }
                //Send the window.setStatus(status) method to the dispatcher that will transfer it across threads
                window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                delegate()
                {
                    window.setStatus(status);
                }));
            }
        }
    }
    public class Artist
    {
        public static int artistCount = 0;
        public int artistID { get; set; }
        public String artistName { get; set; }
        public Boolean saved { get; set; }


        public Artist(String artistName)
        {
            this.artistID = artistCount++;
            this.artistName = artistName;
            this.saved = false;
        }
        public override string ToString()
        {
            return this.artistName;
        }
    }

    public class Song
    {
        public int artistID { get; set; }
        public String videoID { get; set; }
        public String label { get; set; }

        public Song(int artistID, String videoID, String label)
        {
            this.artistID = artistID;
            this.videoID = videoID;
            this.label = label;
        }
        public override string ToString()
        {
            return this.label;
        }
    }

}
