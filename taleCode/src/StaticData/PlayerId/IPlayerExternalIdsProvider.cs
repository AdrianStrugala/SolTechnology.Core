namespace SolTechnology.TaleCode.StaticData.PlayerId;

public interface IPlayerExternalIdsProvider
{
    PlayerIdMap GetExternalPlayerId(int applicationId);
}