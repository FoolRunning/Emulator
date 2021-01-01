namespace SystemBase
{
    public interface IInterruptRequestHandler
    {
        /// <summary>
        /// Interrupt request
        /// </summary>
        void IRQ();

        /// <summary>
        /// Non-maskable interrupt request
        /// </summary>
        void NMI();
    }
}
