using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZakupkiGov.Model
{
    public class BindableObjectBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when some binded Property changes it's Value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged Event.
        /// </summary>
        /// <param name="propertyName">Name of the Property who's Value was changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
