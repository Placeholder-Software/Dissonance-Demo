using System;
using Byn.Net;

namespace Dissonance.Integrations.PureP2P
{
    public struct PureP2PPeer
        : IEquatable<PureP2PPeer>
    {
        public readonly ConnectionId ConnectionId;
        public readonly bool IsLocalLoopback;

        public PureP2PPeer(ConnectionId connectionId)
        {
            ConnectionId = connectionId;
            IsLocalLoopback = false;
        }

        public PureP2PPeer(bool localLoopback)
        {
            ConnectionId = default(ConnectionId);
            IsLocalLoopback = localLoopback;
        }

        public bool Equals(PureP2PPeer other)
        {
            return ConnectionId.Equals(other.ConnectionId)
                && IsLocalLoopback == other.IsLocalLoopback;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is PureP2PPeer && Equals((PureP2PPeer)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ConnectionId.GetHashCode() * 397) ^ IsLocalLoopback.GetHashCode();
            }
        }

        public static bool operator ==(PureP2PPeer left, PureP2PPeer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PureP2PPeer left, PureP2PPeer right)
        {
            return !left.Equals(right);
        }
    }
}
