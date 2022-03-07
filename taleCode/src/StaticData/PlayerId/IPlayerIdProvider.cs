namespace SolTechnology.TaleCode.StaticData.PlayerId;

public interface IPlayerIdProvider
{
    PlayerIdMap GetPlayerId(string name);
}