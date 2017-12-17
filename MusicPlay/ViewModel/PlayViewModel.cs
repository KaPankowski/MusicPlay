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
		Playlists list = new Playlists();

		#region  fields 		
		TimeSpan ts;
		Double _value, _sliderValue = 0;
		Double _volume = 100, maxVolume = 1, minVolume = 0;
		bool _isPlaying, _isPaused = false;
		private bool canexecute = true, israndom = false;
		int _min = 0, _max = 100;		
		private MusicFile musicFile = new MusicFile(0, "", "");		
		private MediaPlayer musicPlayer = new MediaPlayer();
		private DispatcherTimer timer;
		private RelayCommand _IncrementAsBackgroundProcess;			
		#endregion
		#region Properties
			
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
		public ICommand ItemSelected
		{
			get
			{
				return new RelayCommand(param => PlaySelectedSong(list.SelectedIndex));
			}
		}
		public ICommand PlayPreviuos
		{
			get
			{
				return new RelayCommand(param => PlayingPreviuos(canexecute));
			}
		}
		public ICommand PlayNext
		{
			get
			{
				return new RelayCommand(param => PlayingNext(canexecute));
			}
		}
		public ICommand PauseCommand
		{
			get
			{
				return new RelayCommand(param => PausePlaying(canexecute));
			}
		}		
		public ICommand StopCommand
		{
			get
			{
				return new RelayCommand(param => StopPlaying(canexecute));
			}
		}		
		/// <summary>
		/// zwraca metodę PlaySelectedSong
		/// </summary>
		public ICommand PlayCommand
		{
			get
			{
				return new RelayCommand(param => this.PlaySelectedSong(list.SelectedIndex));
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
				if (_value == 100 && list.SelectedIndex + 1 < list.SongList.Count && !israndom) PlaySelectedSong(list.SelectedIndex + 1);
				else if (israndom && _value == 100) PlaySelectedSong(list.SelectedIndex);
				
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
			list.SelectedIndex = list.SongList.IndexOf(list.SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			musicPlayer.Play();
			IncrementProgressBackgroundWorker();
		}
		/// <summary>
		/// Metoda odpowiedzialna za odtwarzanie wybranej ścieżki po podwojnym kliknięciu myszka na ListBox w Interfejsie Użytkownika.
		/// </summary>
		/// <param name="obj"></param>
		private void StartPlayingAfterDoubleClick(object obj)
		{
			PlaySelectedSong(list.SelectedIndex);
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
			list.SelectedIndex = list.SongList.IndexOf(list.SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			int param = 0;
			if (list.SelectedIndex - 1 > 0) param = list.SelectedIndex - 1;
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
			int playlist_length = list.SongList.Count;
			int param = 0;
			list.SelectedIndex = list.SongList.IndexOf(list.SongList.Where(x => x.File_Path == SelectedPath).FirstOrDefault());
			if (list.SelectedIndex + 1 < playlist_length) param = list.SelectedIndex + 1;
			else param = playlist_length - 1;
			PlaySelectedSong(param);
		}
		/// <summary>
		/// Metoda ustawiajaca odpowiedni element Listy obiektu MusicPlay i wywołuje metodę zaczynającą jego odtwarzanie
		/// </summary>
		/// <param name="param">przekazany do metody słuzy do wybrania odpowiedniego obiektu o takim indeksie</param>
		private void PlaySelectedSong(int param)
		{
			
			if (israndom && !_isPaused )
			{
				int tempRand = param;
				SliderValue = 0;
				Random rnd = new Random();				
				param = rnd.Next(0, list.SongList.Count);
				while (param == tempRand)
					param = rnd.Next(0, list.SongList.Count);
			}
			else if (_isPaused)
			{
				_isPaused = false;
				StartPlaying(canexecute);
				return;
			}
			else if (list.SelectedIndex > -1)
			{
				SliderValue = 0;				
			}
			Reset();			
			SelectedPath = list.SongList.ElementAt(param).File_Path;
			SelectedFileName = list.SongList.ElementAt(param).File_Name;
			musicPlayer.Open(new Uri(SelectedPath));
			StartPlaying(canexecute);

		}
		private void DidUserChooseRandom()
		{
			if (israndom) israndom = false;
			else if (!israndom) israndom = true;
		}
		#endregion
	}
}
