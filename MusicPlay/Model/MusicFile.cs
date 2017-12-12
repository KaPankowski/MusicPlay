using MusicPlay.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlay.Model
{
	class MusicFile : ObservedObject
	{
		private string file_path = "";
		private string file_name = "";
		private int file_index;
		

		public MusicFile(int id,string path,string name) 
		{
			file_path = path;
			file_name = name;
			file_index = id;
		}

		public string File_Path
		{
			get
			{
				return file_path;
			}
			set
			{
				if (value != file_path)
				{
					file_path = value;
					OnPropertyChanged("File_Path");
				}
			}
		}
		public string File_Name
		{
			get
			{
				return file_name;
			}
			set
			{
				if (value != file_name)
				{
					file_name = value;
					OnPropertyChanged("File_Name");
				}
			}
		}
		public int File_Index
		{
			get
			{
				return file_index;
			}
			set
			{
				if (value != file_index)
				{
					file_index = value;
					OnPropertyChanged("File_Name");
				}
			}
		}
	}
}
