namespace SystemBase
{
    public interface ISoundChannelGenerator
    {
        float GetSample(int channel, float globalTime, float timeStep);
    }
}
