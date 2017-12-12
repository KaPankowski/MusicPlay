using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlay.ViewModel
{
	class ObservedObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(params string[] properties)
		{
			if (PropertyChanged != null)
			{
				foreach (var property in properties)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(property));
				}
			}
		}
	}
}
