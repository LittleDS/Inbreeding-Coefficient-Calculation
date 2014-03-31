using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGDL
{
    class Triple
    {
        public String source { get; set; }
        public String destination { get; set; }
        public char Gender { get; set; }

        public Triple(String s, String d, char G)
        {
            source = s;
            destination = d;
            Gender = G;
        }

        public bool Equals(Triple other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return source == other.source && destination == other.destination;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (Triple)) return false;
            return Equals((Triple) obj);
        }
  
        public static bool operator ==(Triple left, Triple right)
        {
            return Equals(left, right);
        }
  
        public static bool operator !=(Triple left, Triple right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return source.GetHashCode() + destination.GetHashCode();
        }

    }
}
