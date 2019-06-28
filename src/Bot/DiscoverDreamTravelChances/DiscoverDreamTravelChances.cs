namespace DreamTravel.Bot.DiscoverDreamTravelChances
{
    using Infrastructure.Email;
    using Interfaces;
    using SendEmail;
    using System.Collections.Generic;
    using Models;

    public class DiscoverDreamTravelChances : IDiscoverDreamTravelChances
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IGetUsers _getUsers;
        private readonly IScrapHtmlToChanceModel _scrapHtmlToChanceModel;
        private readonly IFilterChances _filterChances;

        public DiscoverDreamTravelChances(IComposeMessage composeMessage, IGetUsers getUsers, IScrapHtmlToChanceModel scrapHtmlToChanceModel, IFilterChances filterChances)
        {
            _composeMessage = composeMessage;
            _getUsers = getUsers;
            _scrapHtmlToChanceModel = scrapHtmlToChanceModel;
            _filterChances = filterChances;
        }

        public void Execute()
        {
            List<Chance> chances = _scrapHtmlToChanceModel.Execute();

            chances = _filterChances.Execute(chances);

            var users = _getUsers.Execute();

            foreach (var user in users)
            {
                string message = _composeMessage.ExecuteHtml(chances, user.Name);
                EmailAgent.Send(new DreamTravelChanceEmail(message, user.Email));
            }
        }
    }
}
