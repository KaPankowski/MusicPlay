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

namespace MusicPlay.ViewModel
{
	class Playlists 
	{
		

		public Playlists()
		{

		}

		PlayViewModel play = new PlayViewModel();

		public static void NameThePlaylist()
		{

		} 

		public static void SaveTextFile(ObservableCollection<MusicFile> list)
		{
			string path = ChoosePlaylistNameAndPath();
			if (list.Count>0)
			{
				using (TextWriter write = new StreamWriter(path))
				{
					foreach (var item in list)
					{
						write.WriteLine(string.Format("{0};{1}", item.File_Path, item.File_Name));
					}
				}
			}			
		}
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
					playlist.Add(new MusicFile(id++,item[0], item[1]));
				}
			}
			return playlist;
		}

		public static  string ChoosePlaylistNameAndPath()
		{
			SaveFileDialog savePlayList = new SaveFileDialog();
			savePlayList.Filter = "txt.files (*.txt|*.txt|all files (*.*)|*.*)";
			savePlayList.Title = "Nazwa twojej PlayListy";
			savePlayList.ShowDialog();
			string path = savePlayList.FileName;
			return path;
		} 
		

		
	}
}
