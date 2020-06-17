namespace GameCraft
{
    public interface IBlock
    {
        bool IsTranslucent { get; }

        ushort? Index { get; }
    }
}
