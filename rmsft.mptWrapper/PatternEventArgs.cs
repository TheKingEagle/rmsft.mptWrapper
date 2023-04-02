namespace rmsft.mptWrapper
{
    public class PatternEventArgs
    {
        public int Pattern { get; private set; }
        public int Order { get; private set; }
        public int Row { get; private set; }
        public int SubSong { get; private set; }

        public PatternEventArgs(int p, int o,int r, int s)
        {
            Pattern = p;
            Order = o;
            Row = r;
            SubSong = s;
        }
    }
}