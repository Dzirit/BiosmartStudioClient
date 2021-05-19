using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiosmarStudioClient
{
    public class Template
    {
        public int TemplateId { get; set; }
        public byte[] Sample { get; set; }
        public int Type { get; set; }
        public int Quality { get; set; }
        public int UserId { get; set; }
    }
}
