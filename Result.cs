public class Result
{
    public List<bool> PageFaults { get; set; }
    public int PageFaultsCount { get; set; }
    public Dictionary<int, List<int>>? Frames { get; set; }
    public Dictionary<int, List<List<int>>>? ClockFrames { get; set; }

    public Result(List<bool> pageFaults, int pageFaultsCount, Dictionary<int, List<int>>? frames)
    {
        PageFaults = pageFaults;
        PageFaultsCount = pageFaultsCount;
        Frames = frames;
    }

    public Result(
        List<bool> pageFaults,
        int pageFaultsCount,
        Dictionary<int, List<List<int>>>? clockFrames
    )
    {
        PageFaults = pageFaults;
        PageFaultsCount = pageFaultsCount;
        ClockFrames = clockFrames;
    }
}
