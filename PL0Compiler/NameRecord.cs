using PL0Resources;

namespace PL0Compiler
{
	public class NameRecord
    {
        public Token Kind { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public int? Level { get; set; }
        public int? Address { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objR = (NameRecord)obj;

            return Name == objR.Name && Kind == objR.Kind && objR.Level == Level && Address == objR.Address;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Choose large primes to avoid hashing collisions
                const int hashingBase = (int)2166136261;
                const int hashingMultiplier = 16777619;

                var hash = hashingBase;
                hash = (hash * hashingMultiplier) ^ (!ReferenceEquals(null, Name) ? Name.GetHashCode() : 0);
                hash = (hash * hashingMultiplier) ^ (!ReferenceEquals(null, Level) ? Level.GetHashCode() : 0);
                return hash;
            }
        }

        public static bool operator == (NameRecord a, NameRecord b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return a?.Equals(b) == true;
        }

        public static bool operator !=(NameRecord a, NameRecord b)
        {
            return !(a == b);
        }
    }
}
