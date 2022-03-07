namespace SolTechnology.TaleCode.StaticData;

public interface IPlayerIdProvider
{
    PlayerIdMap GetPlayerId(string name);
}