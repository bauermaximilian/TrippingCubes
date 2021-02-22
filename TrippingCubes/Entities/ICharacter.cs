namespace TrippingCubes.Entities
{
    interface ICharacter : IEntity
    {
        int HealthPoints { get; set; }

        bool IsInvisible { get; }
    }
}
