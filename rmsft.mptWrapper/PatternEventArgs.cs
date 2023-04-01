namespace rmsft.mptWrapper
{
    public class PatternEventArgs
    {
        public int Pattern { get; private set; }
        public int Order { get; private set; }

        public PatternEventArgs(int p, int o)
        {
            Pattern = p;
            Order = o;
        }
    }
}