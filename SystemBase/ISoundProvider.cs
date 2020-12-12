namespace SystemBase
{
    public interface ISoundProvider
    {
        int ChannelCount { get; }

        //IEnumerable<ISoundChannelGenerator> InputChannels { get; }
        float GetSample(int channel, float globalTime, float timeStep);
    }
}
