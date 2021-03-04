using System;
using System.Collections.Generic;
using System.Text;

namespace BiosmartStudioClient
{
    class PalmTemplate
    {
        public string UserId { get; set; }
        public string Template { get; set; }
        public int Quality { get; set; }
        public int HandType { get; set; } = 101;//only right hand//100-left, 101-right
    }
}
