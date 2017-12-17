using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MusicPlay.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media;
using System.Threading;

namespace MusicPlay.ViewModel
{
	class Playlists : ObservedObject
	{
		private bool canexecute = true;
		static MediaPlayer musicPlayer = new MediaPlayer();		
		private static ObservableCollection<MusicFile>  songList = new ObservableCollection<MusicFile>();
		 
		private static int selectedIndex;
		public int SelectedIndex
		{
			get
			{
				return selectedIndex;
			}

			set
			{
				selectedIndex = value;
				OnPropertyChanged("SelectedIndex");
			}
		}
		public  ObservableCollection<MusicFile> SongList
		{
			get
			{
				return songList;
			}

			set
			{
				if (value != songList)
				{
					songList = value;
					OnPropertyChanged("SongList");
				}
			}
		}
		public ICommand LoadToSongList
		{
			get
			{
				return new RelayCommand(param => this.LoadPlayListFromFileToSongList());
			}
		}
		public ICommand BrowseMusicFIle
		{
			get
			{
				return new RelayCommand(param => this.Browse(canexecute));
			}
		}
		public ICommand DeleteSong
		{
			get
			{
				return new RelayCommand(param => this.DeleteSongFromList());
			}
		}
		public ICommand SavePlayList
		{
			get
			{
				return new RelayCommand(param => this.SaveToTextFile(SongList));
			}
		}
		public ICommand ClearCommand
		{
			get
			{
				return new RelayCommand(param => SongList.Clear());
			}
		}

		public static void NameThePlaylist()
		{

		}
		/// <summary>
		/// pobiera ścieżkę gdzie znajduje się lista i dodaje jej elementy do SongList
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static ObservableCollection<MusicFile> LoadPlaylist(string path)
		{
			int id = 1;
			ObservableCollection<MusicFile> playlist = new ObservableCollection<MusicFile>();
			
			string line;
			
			using (StreamReader str = new StreamReader(path))
			{
				while((line = str.ReadLine()) != null)
				{
					string[] item = line.Split(';');
					playlist.Add(new MusicFile(id++,item[0], item[1], (TimeSpan.TryParse(item[2],out TimeSpan ts)? ts : default(TimeSpan))));
				}
			}
			return playlist;
		}
		/// <summary>
		/// Pobiera od użytkownika nazwę playlisty i miesjce gdzie ma być zapisana
		/// </summary>
		/// <returns></returns>
		public static  string ChoosePlaylistNameAndPath()
		{
			SaveFileDialog savePlayList = new SaveFileDialog();
			savePlayList.Filter = "txt.files (*.txt|*.txt|all files (*.*)|*.*)";
			savePlayList.Title = "Nazwa twojej PlayListy";
			savePlayList.ShowDialog();
			string path = savePlayList.FileName;
			return path;
		}
		/// <summary>
		/// Zapisuje liste do pliku
		/// </summary>
		/// <param name="list"></param>
		public void SaveToTextFile(ObservableCollection<MusicFile> list)
		{
			string path = ChoosePlaylistNameAndPath();
			if (list.Count > 0)
			{
				using (TextWriter write = new StreamWriter(path))
				{
					foreach (var item in list)
					{
						write.WriteLine(string.Format("{0};{1};{2}", item.File_Path, item.File_Name,item.File_Time));
					}
				}
			}
		}
		/// <summary>
		/// Dodaje dane pliku muzycznego do SongList
		/// </summary>
		private void LoadPlayListFromFileToSongList()
		{
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Filter = "txt.files (*.txt|*.txt|all files (*.*)|*.*)";
			if (openDialog.ShowDialog() == true)
			{
				string path = openDialog.FileName;
				SongList = Playlists.LoadPlaylist(path);
			}
		}
		/// <summary>
		/// Metoda słuzy do wybierania plików do playlisty. 
		/// Po wybraniu pliku dodaje do Listy obiektu MusicFile ścieżkę pliku i jego nazwę.
		/// Zmienia również na liście uzytkownika zaznaczony wlement na ostatni dodany.
		/// </summary>
		/// <param name="obj"></param>
		private void Browse(object obj)
		{
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Filter = "MP3.files (*.mp3|*.mp3|all files (*.*)|*.*)";
			openDialog.Multiselect = true;

			if (openDialog.ShowDialog() == true)
			{
				AddToSongList(openDialog);				
			}
		}
		/// <summary>
		/// metoda dodaje najpierw do list zastepczych sciezke, nazwe i czas.
		/// 
		/// </summary>
		/// <param name="openFile"></param>
		private void AddToSongList(OpenFileDialog openFile)
		{
			int index = 0;
			ObservableCollection<string> path = new ObservableCollection<string>();
			ObservableCollection<string> name = new ObservableCollection<string>();
			ObservableCollection<TimeSpan> timelist = new ObservableCollection<TimeSpan>();
			MediaPlayer pl = new MediaPlayer();
			TimeSpan ts = new TimeSpan();

			foreach (var item in openFile.FileNames)
			{
				path.Add(item);
			}
			foreach (var item in openFile.SafeFileNames)
			{
				name.Add(item);
			}

			if (SongList.Count > 0) index = SongList.Count;

			
			for (int i = 0; i < path.Count; i++)
			{
				pl.Open(new Uri(path[i]));
			
				do
				{
					if (pl.NaturalDuration.HasTimeSpan)
					{
						ts = pl.NaturalDuration.TimeSpan;
						ts = GetRoundedTimeSpan(ts);
						timelist.Add(ts);
					}

				} while (timelist.Count <= i);

			}
			for (int i = 0; i < timelist.Count; i++)
			{
				SongList.Add(new MusicFile(++index, path[i], name[i], timelist[i]));
			}

		}

		private TimeSpan GetRoundedTimeSpan(TimeSpan notRounded)
		{
			//int precision = 0;
			int size = 7;
			int factor = (int)Math.Pow(10,size);
			TimeSpan roundedTimeSpan = new TimeSpan(((long)Math.Round((1.0 * notRounded.Ticks / factor)) * factor));
			return roundedTimeSpan;

		}


		/// <summary>
		/// pobiera index wybranego elementu z listboxa i go usuwa
		/// </summary>
		private void DeleteSongFromList()
		{
			if (SongList.Count > 0 && SelectedIndex >= 0)
			{
				SongList.RemoveAt(SelectedIndex);
			}
		}


	}
}
