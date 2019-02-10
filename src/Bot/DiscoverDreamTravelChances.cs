namespace DreamTravel.Bot
{
    using Scrap_AzairEu;
    using SendEmail;
    using SendEmail.Interfaces;
    using System.Collections.Generic;

    public class DiscoverDreamTravelChances : IDiscoverDreamTravelChances
    {
        private readonly IComposeMessage _composeMessage;
        private readonly IEmailAgent _emailAgent;
        private readonly IProvideRecipients _provideRecipients;
        private readonly IScrapHtmlToChanceModel _scrapHtmlToChanceModel;
        private readonly IFilterChances _filterChances;

        public DiscoverDreamTravelChances(IComposeMessage composeMessage, IEmailAgent emailAgent, IProvideRecipients provideRecipients, IScrapHtmlToChanceModel scrapHtmlToChanceModel, IFilterChances filterChances)
        {
            _composeMessage = composeMessage;
            _emailAgent = emailAgent;
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
                _emailAgent.Send(new DreamTravelChanceEmail(message, recipient.Email));
            }
        }
    }
}
