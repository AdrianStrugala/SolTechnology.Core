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
        private readonly IProvideRecipients _provideRecipients;
        private readonly IScrapHtmlToChanceModel _scrapHtmlToChanceModel;
        private readonly IFilterChances _filterChances;

        public DiscoverDreamTravelChances(IComposeMessage composeMessage, IProvideRecipients provideRecipients, IScrapHtmlToChanceModel scrapHtmlToChanceModel, IFilterChances filterChances)
        {
            _composeMessage = composeMessage;
            _provideRecipients = provideRecipients;
            _scrapHtmlToChanceModel = scrapHtmlToChanceModel;
            _filterChances = filterChances;
        }

        public void Execute()
        {
            List<Chance> chances = _scrapHtmlToChanceModel.Execute();

            chances = _filterChances.Execute(chances);

            string message = _composeMessage.ExecuteHtml(chances);

            foreach (var recipient in _provideRecipients.Execute())
            {
                EmailAgent.Send(new DreamTravelChanceEmail(message, recipient.Email));
            }
        }
    }
}
