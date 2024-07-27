namespace SerialLoops.Lib.Util
{
    public interface IProgressTracker
    {
        public int Finished { get; set; } 
        public int Total { get; set; }
        public string CurrentlyLoading { get; set; }

        public void Focus(string item, int count)
        {
            Total = count;
            Finished = 0;
            CurrentlyLoading = item;
        }

    }
}
