namespace SystemBase
{
    public interface IBus
    {
        void Reset();

        void IRQ<T>() where T : IInterruptRequestHandler;

        void NMI<T>() where T : IInterruptRequestHandler;
    }
}
