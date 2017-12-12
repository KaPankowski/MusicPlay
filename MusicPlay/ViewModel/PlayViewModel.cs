using Microsoft.Win32;
using MusicPlay.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MusicPlay.ViewModel
{
	class PlayViewModel : ObservedObject
	{
		#region  fields 		
		TimeSpan ts;
		Double _value, _sliderValue = 0;
		Double _volume = 100, maxVolume = 1, minVolume = 0;
		bool _isPlaying, _isPaused = false;
		private bool canexecute = true, israndom = false;
		int _min = 0, _max = 100, _selected_index;
		private MusicFile musicFile = new MusicFile(0, "", "");
		protected static ObservableCollection<MusicFile> songList = new ObservableCollection<MusicFile>();
		private MediaPlayer musicPlayer = new MediaPlayer();
		private DispatcherTimer timer;
		private RelayCommand _IncrementAsBackgroundProcess;
		private ICommand _openCommand;
		private ICommand _playCommand;
		private ICommand _stopCommand;
		private ICommand _pauseCommand;
		private ICommand _playNextCommand;
		private ICommand _playPreviousCommand;
		private ICommand _saveList;
		private ICommand _loadListCommand;
		#endregion

		#region Properties
		public MusicFile FileSong
		{
			get
			{
				return musicFile;
			}
			set
			{
				if (value != musicFile)
				{
					musicFile = value;
					OnPropertyChanged("FileSong");
				}
			}
		}
		public RelayCommand ItemSelected { get; set; }
		public int Selected_Index
		{
			get
			{
				return _selected_index;
			}
			set
			{
				if (_selected_index != value)
				{

					_selected_index = value;

					OnPropertyChanged("Selected_Index");
				}
			}
		}
		public ObservableCollection<MusicFile> SongList
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
		public string SelectedPath
		{
			get { return musicFile.File_Path; }
			set
			{

				musicFile.File_Path = value;
				OnPropertyChanged("SelectedPath");
			}
		}
		public string SelectedFileName
		{
			get
			{
				return musicFile.File_Name;
			}
			set
			{
				musicFile.File_Name = value;
				OnPropertyChanged("SelectedFileName");
			}

		}
		#region ICommand properties
		public ICommand LoadPlaylist
		{
			get
			{
				return _loadListCommand;
			}
			set
			{
				_loadListCommand = value;
			}
		}
		public ICommand PlayPreviuos
		{
			get { return _playPreviousCommand; }
			set
			{
				_playPreviousCommand = value;
			}
		}
		public ICommand PlayNext
		{
			get { return _playNextCommand; }
			set
			{
				_playNextCommand = value;
			}
		}
		public ICommand PauseCommand
		{
			get
			{
				return _pauseCommand;
			}
			set
			{
				_pauseCommand = value;
			}
		}
		public ICommand SaveList
		{
			get
			{
				return _saveList;
			}
			set
			{
				_saveList = value;
			}
		}
		public ICommand StopCommand
		{
			get
			{

				return _stopCommand;
			}
			set
			{
				_stopCommand = value;
			}
		}
		public ICommand OpenCommand
		{
			get
			{
				return _openCommand;
			}
			set
			{
				_openCommand = value;
			}
		}
		/// <summary>
		/// zwraca metodę PlaySelectedSong
		/// </summary>
		public ICommand PlayCommand
		{
			get
			{

				return new RelayCommand(param => this.PlaySelectedSong(Selected_Index));

			}
		}
		/// <summary>
		/// przesyła komendę o zamknięciu aplikacji
		/// </summary>
		public ICommand ClosePlayer
		{
			get
			{
				return new RelayCommand(param => { (param as System.Windows.Window).Close(); });
			}
		}
		public ICommand DeleteSongCommand
		{
			get
			{
				return new RelayCommand(param => this.DeleteSongFromList());
			}
		}
		public ICommand IncrementAsBackgroundProcess
		{
			get
			{
				if (_IncrementAsBackgroundProcess == null)
				{
					_IncrementAsBackgroundProcess = new RelayCommand(param => this.IncrementProgressBackgroundWorker());
				}
				return _IncrementAsBackgroundProcess;
			}
		}
		public ICommand IsRandomCommand
		{
			get
			{
				return new RelayCommand(param => this.DidUserChooseRandom());
			}
		}
		public ICommand ClearCommand
		{
			get
			{
				return new RelayCommand(param => SongList.Clear());
			}
		}
		#endregion
		#region properties for progress bar

		public bool IsPlaying
		{
			get
			{
				return _isPlaying;
			}
			set
			{
				_isPlaying = value;
				OnPropertyChanged("IsPlaying");
				OnPropertyChanged("IsNotPlaying");
			}
		}
		public bool IsNotPlaying
		{
			get { return !IsPlaying; }
		}
		public int Max
		{
			get { return _max; }
			set { _max = value; OnPropertyChanged("Max"); }
		}
		public int Min
		{
			get { return _min; }
			set { _min = value; OnPropertyChanged("Min"); }
		}
		public double MaxVolume
		{
			get
			{
				return maxVolume;
			}
			set
			{
				maxVolume = value;
			}
		}
		public double MinVolume
		{
			get
			{
				return minVolume;
			}
			set
			{
				minVolume = value;
			}
		}
		/// <summary>
		/// if #1 jeżeli wartość SliderValue większa od _value to ustal nową wartość value i zacznij odtwarzanie od tego momentu
		/// if #2 jeseli wartosc SliderValue mniejsza od _value - 2 to ustal nową wartość value i zacznij odtwarzanie od tego momentu
		/// if #3 _value=100 i Selected_Index+1 mniejszy od długości Listy SongList, lub israndom jest tru i _value = 100 odtwórz następny utwór
		/// </summary>
		public Double Value
		{
			get
			{
				if (SliderValue > _value)
				{
					_value = _sliderValue;
					int param = (int)(_value * ts.TotalMilliseconds / 100);
					TimeSpan tempTs = new TimeSpan(0, 0, 0, 0, param);
					musicPlayer.Position = tempTs;
					return _value;
				}
				else if (SliderValue < _value - 2)
				{
					_value = _sliderValue;
					int param = (int)(_value * ts.TotalMilliseconds / 100);
					TimeSpan tempTs = new TimeSpan(0, 0, 0, 0, param);
					musicPlayer.Position = tempTs;
					return _value;
				}
				if (_value == 100 && Selected_Index + 1 < SongList.Count && !israndom) PlaySelectedSong(Selected_Index + 1);
				else if (israndom && _value == 100) PlaySelectedSong(Selected_Index);
				if (_isPaused) return _value;
				 //|| israndom && _value == 100
				SliderValue = _value;
				return _value;
			}
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}
		public Double Volume
		{
			get
			{

				musicPlayer.Volume = _volume;
				return _volume;
			}
			set
			{

				_volume = value;
				OnPropertyChanged("Volume");

			}
		}
		public double SliderValue
		{
			get
			{
				return _sliderValue;
			}
			set
			{
				_sliderValue = value;

				OnPropertyChanged("SliderValue");
			}
		}
		#endregion
		#endregion

		#region konstruktor
		public PlayViewModel()
		{
			OpenCommand = new RelayCommand(Browse, param => this.canexecute);
			StopCommand = new RelayCommand(StopPlaying, param => this.canexecute);
			PauseCommand = new RelayCommand(PausePlaying, param => this.canexecute);
			PlayNext = new RelayCommand(PlayingNext, param => this.canexecute);
			PlayPreviuos = new RelayCommand(PlayingPreviuos, param => this.canexecute);
			ItemSelected = new RelayCommand(StartPlayingAfterDoubleClick);
			SaveList = new RelayCommand(SavePlaylistToFIle, param => this.canexecute);
			LoadPlaylist = new RelayCommand(LoadPlayListFromFile, param => this.canexecute);
		}
		#endregion

		#region metody odpowiedzialne za progreesbar i slider
		private void Reset()
		{
			Value = Min;
		}
		/// <summary>
		/// Metoda odpowiadająca za aktualizację wartości Value;
		/// Jeśli IsPlaying=false Resetuje Value i uruchamia timer, który aktualizuje postęp 
		/// progressbar i suwaka w określonych odstepach czasu. 
		/// </summary>
		public void IncrementProgressBackgroundWorker()
		{
			if (IsPlaying)
				return;

			Reset();
			IsPlaying = true;

			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(1);
			timer.Tick += new EventHandler(timerTick);
			timer.Start();
		}
		/// <summary>
		/// metoda, która aktualizuje bezpośrednio wartość Value 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void timerTick(object sender, EventArgs e)
		{
			//var dispatcher = Application.Current.Dispatcher;
			//dispatcher.BeginInvoke((Action)(() =>
			//{				
			//	if (musicPlayer.Source != null && musicPlayer.NaturalDuration.HasTimeSpan)
			//	{
			//		ts = musicPlayer.NaturalDuration.TimeSpan;
			//		Value = ((double)musicPlayer.Position.TotalMilliseconds / ts.TotalMilliseconds) * 100;
			//	}
			//}
			//));
			if (musicPlayer.Source != null && musicPlayer.NaturalDuration.HasTimeSpan)
			{
				ts = musicPlayer.NaturalDuration.TimeSpan;
				Value = ((double)musicPlayer.Position.TotalMilliseconds / ts.TotalMilliseconds) * 100;
			}
		}
		/// <summary>
		/// metoda ustawiajaca wartość IsPlaying na false.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ItsNotPlaying(object sender, RunWorkerCompletedEventArgs e)
		{
			IsPlaying = false;
		}
		#endregion

		#region metody odpowiedzialne na odtwarzanie
		/// <summary>
		/// Metoda odpowiedzialna za zaczęcie odtwarzania
		/// </summary>
		/// <param name="obj"></param>
		private void StartPlaying(object obj)
		{
			Selected_Index = SongList.IndexOf(SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			musicPlayer.Play();
			IncrementProgressBackgroundWorker();
		}
		/// <summary>
		/// Metoda odpowiedzialna za odtwarzanie wybranej ścieżki po podwojnym kliknięciu myszka na ListBox w Interfejsie Użytkownika.
		/// </summary>
		/// <param name="obj"></param>
		private void StartPlayingAfterDoubleClick(object obj)
		{
			PlaySelectedSong(Selected_Index);
		}
		/// <summary>
		/// zatrzymuję odtwarzanie i resetuje SliderValue do 0;
		/// </summary>
		/// <param name="obj"></param>
		private void StopPlaying(object obj)
		{
			SliderValue = 0;
			musicPlayer.Stop();
		}
		/// <summary>
		/// Pauzuje odtwarzanie
		/// </summary>
		/// <param name="obj"></param>
		private void PausePlaying(object obj)
		{
			_isPaused = true;
			musicPlayer.Pause();

		}
		/// <summary>
		/// <param name="param">zmienna w której ustawia się wartość indeksu w zależności od wykonywanej metody</param>
		/// Jeżeli wartość indeksu zaznaczonego obiektu Listy - 1 jest większa od 0
		/// można przejść do poprzedniego utworu, param ustawiony na wartość indeksu zaznaczonego obiektu - 1
		/// Inaczej param= 0, dzięki czemu wykroczy poza zakres listy
		/// </summary>
		/// <param name="obj"></param>
		private void PlayingPreviuos(object obj)
		{
			Selected_Index = SongList.IndexOf(SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			int param = 0;
			if (Selected_Index - 1 > 0) param = Selected_Index - 1;
			else param = 0;
			PlaySelectedSong(param);
		}
		/// <summary>
		/// <param name="playlist_lenght">Obecna długość playlisty</param>
		/// <param name="param">zmienna w której ustawia się wartość indeksu w zależności od wykonywanej metody</param>
		/// Jeżeli wartość indeksu zaznaczonego obiektu Listy + 1 jest mniejsza niż długość tej listy
		/// można przejść do kolejnego utworu, param ustawiony na wartość indeksu zaznaczonego obiektu + 1
		/// Inaczej param= ostatni element listy, dzięki czemu nie przekroczy jej zakresu
		/// </summary>
		/// <param name="obj"></param>
		private void PlayingNext(object obj)
		{
			int playlist_length = SongList.Count;
			int param = 0;
			Selected_Index = SongList.IndexOf(SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			if (Selected_Index + 1 < playlist_length) param = Selected_Index + 1;
			else param = playlist_length - 1;
			PlaySelectedSong(param);


		}
		/// <summary>
		/// Metoda ustawiajaca odpowiedni element Listy obiektu MusicPlay i wywołuje metodę zaczynającą jego odtwarzanie
		/// </summary>
		/// <param name="param">przekazany do metody słuzy do wybrania odpowiedniego obiektu o takim indeksie</param>
		private void PlaySelectedSong(int param)
		{
			
			if (israndom)
			{
				int tempRand = param;
				SliderValue = 0;
				Random rnd = new Random();
				param = rnd.Next(0, SongList.Count);
				while(param == tempRand)
					param = rnd.Next(0, SongList.Count);


			}
			else if (_isPaused)
			{
				StartPlaying(canexecute);
				return;
			}
			else if (Selected_Index > -1)
			{
				SliderValue = 0;				
			}
			Reset();
			SelectedPath = SongList.ElementAt(param).File_Path;
			SelectedFileName = SongList.ElementAt(param).File_Name;
			musicPlayer.Open(new Uri(SelectedPath));
			StartPlaying(canexecute);

		}
		private void DidUserChooseRandom()
		{
			if (israndom) israndom = false;
			else if (!israndom) israndom = true;
		}
		#endregion

		#region Metody odpowiedzialne za playlistę
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
			int index = 0;
			ObservableCollection<string> path = new ObservableCollection<string>();
			ObservableCollection<string> name = new ObservableCollection<string>();

			if (openDialog.ShowDialog() == true)
			{
				foreach (var item in openDialog.FileNames)
				{
					path.Add(item);
				}
				foreach (var item in openDialog.SafeFileNames)
				{
					name.Add(item);
				}

				if (SongList.Count > 0) index = songList.Count;
				;
				
				for (int i = 0; i < name.Count; i++)
				{
					SongList.Add(new MusicFile(++index, path[i], name[i]));
				}
			}
		}
		/// <summary>
		/// pobiera index wybranego elementu z listboxa i go usuwa
		/// </summary>
		private void DeleteSongFromList()
		{
			if (SongList.Count > 0 && Selected_Index >= 0)
			{
				SongList.RemoveAt(Selected_Index);
			}
		}
		private void SavePlaylistToFIle(object obj)
		{
			Playlists.SaveTextFile(SongList);
		}
		private void LoadPlayListFromFile(object obj)
		{
			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Filter = "txt.files (*.txt|*.txt|all files (*.*)|*.*)";
			if (openDialog.ShowDialog() == true)
			{
				string path = openDialog.FileName;
				SongList = Playlists.LoadPlaylist(path);
			}
		}
		
		#endregion
	}



}
