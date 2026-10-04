namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Constant-space cast/sequence gate. End retires a cast even when its
    /// Begin was lost; late Launch/Confirm/Begin can never resurrect it.</summary>
    internal sealed class GazeFuelSequence
    {
        private uint cast, sequence;
        private bool active;
        public bool Accept(uint id, uint seq, bool begin, bool end)
        {
            if (id == 0 || seq == 0) return false;
            if (id != cast)
            {
                if (cast != 0 && unchecked((int)(id - cast)) <= 0) return false;
                if (!begin && !end) return false;
                cast = id; sequence = seq; active = !end;
                return true;
            }
            if (!active || seq <= sequence || begin) return false;
            sequence = seq;
            if (end) active = false;
            return true;
        }
        public void Retire() { active = false; }
    }
}
