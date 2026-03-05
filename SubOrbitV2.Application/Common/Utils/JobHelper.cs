namespace SubOrbitV2.Application.Common.Utils;

public static class JobHelper
{
    /// <summary>
    /// Nexi Mutabakat Bekçisi (Status Checker) için dinamik bekleme süresi hesaplar.
    /// Min: 5 dk, Max: 60 dk, Her 100 işlemde +1 dk.
    /// </summary>
    public static TimeSpan CalculateStatusCheckDelay(int itemCount)
    {
        int delayMinutes = Math.Max(5, (int)Math.Ceiling(itemCount / 100.0));
        delayMinutes = Math.Min(delayMinutes, 60);

        return TimeSpan.FromMinutes(delayMinutes);
    }

    /// <summary>
    /// Binlerce veriyi chunk'lara böldüğümüzde, her bir chunk için kaç dakika sonra
    /// job kurulacağını hesaplar. (Örn: 1. chunk 0 dk, 2. chunk 5 dk, 3. chunk 10 dk)
    /// </summary>
    public static TimeSpan CalculateChunkDispatchDelay(int loopIndex, int chunkSize, int delayMinutesPerChunk = 5)
    {
        int delay = (loopIndex / chunkSize) * delayMinutesPerChunk;
        return TimeSpan.FromMinutes(delay);
    }
}