namespace DreamTravel.Bot.DiscoverDreamTravelChances.SendEmail
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using ScrapAzairEu;

    public class FilterChances : IFilterChances
    {
        private readonly Dictionary<string, int> daysOfTheWeek = new Dictionary<string, int>
        {
            {"Mon", 0},
            {"Tue", 1},
            {"Wed", 2},
            {"Thu", 3},
            {"Fri", 4},
            {"Sat", 5},
            {"Sun", 6},

        };

        public List<Chance> Execute(List<Chance> chances)
        {
            chances = chances.Where(c => c.Price < 50).ToList();

            chances = chances.Where(
                c => ContainsWeekend(
                    daysOfTheWeek[c.ThereDate.Remove(3)],
                    daysOfTheWeek[c.BackDate.Remove(3)])
                ).ToList();


            return chances;
        }

        private bool ContainsWeekend(int day1, int day2)
        {

            if (day2 > day1)
            {
                if (day2 > daysOfTheWeek["Fri"])
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }
    }
}
