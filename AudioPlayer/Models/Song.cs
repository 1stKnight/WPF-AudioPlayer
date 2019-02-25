using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayer.Models
{
    class Song
    {
       public string Name { get; set; }
       public string Duration { get; set; }

        public Song(string Name, string Duration)
        {
            this.Name = Name;
            this.Duration = Duration;
        }
    }
}
