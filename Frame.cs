namespace PageReplacement.Models
{
    public class Frame
    {
        public int FrameNumber { get; set; }
        public int? NextUseTime { get; set; }

        public Frame(int frameNumber, int? nextUseTime)
        {
            FrameNumber = frameNumber;
            NextUseTime = nextUseTime;
        }
    }
}
