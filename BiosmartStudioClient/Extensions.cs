using System.Collections.Generic;
using System.Xml.Linq;


namespace BiosmarStudioClient
{
    public static class Extensions
    {
        public static bool ContainsFIO(this string s)
        {
            return s.Split(' ').Length == 3;
        }

        public static IEnumerable<XElement> ElementsOrNull(this XElement element, XName name)
        {
            return element.HasElements ?
                    element.Elements(name)
                   : null;
        }
    }
}
