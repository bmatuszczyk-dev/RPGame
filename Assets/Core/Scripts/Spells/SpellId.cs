using System;

namespace RPGame.Core.Spells
{
    public readonly struct SpellId : IEquatable<SpellId>
    {
        public SpellId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SpellId other)
        {
            return string.Equals(ToString(), other.ToString(), StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SpellId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ToString());
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(SpellId left, SpellId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpellId left, SpellId right)
        {
            return !left.Equals(right);
        }
    }
}
