namespace SolTechnology.TaleCode.StaticData.PlayerId;

public interface IPlayerExternalIdsProvider
{
    PlayerIdMap Get(int applicationId);
}