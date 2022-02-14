namespace ApiClients.FootballDataApi;

public class PlayerModel
{
    public int Count { get; set; }
    public Filters Filters { get; set; }
    public Player Player { get; set; }
    public List<Match> Matches { get; set; }
}

public class Filters
{
    public string Permission { get; set; }
    public int Limit { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DateOfBirth { get; set; }
    public string CountryOfBirth { get; set; }
    public string Nationality { get; set; }
    public string Position { get; set; }
    public object ShirtNumber { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class Competition
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Season
{
    public int Id { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public int CurrentMatchday { get; set; }
}

public class Odds
{
    public string Msg { get; set; }
}

public class FullTime
{
    public int HomeTeam { get; set; }
    public int AwayTeam { get; set; }
}

public class HalfTime
{
    public int HomeTeam { get; set; }
    public int AwayTeam { get; set; }
}

public class ExtraTime
{
    public object HomeTeam { get; set; }
    public object AwayTeam { get; set; }
}

public class Penalties
{
    public object HomeTeam { get; set; }
    public object AwayTeam { get; set; }
}

public class Score
{
    public string Winner { get; set; }
    public string Duration { get; set; }
    public FullTime FullTime { get; set; }
    public HalfTime HalfTime { get; set; }
    public ExtraTime ExtraTime { get; set; }
    public Penalties Penalties { get; set; }
}

public class HomeTeam
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class AwayTeam
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Referee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Nationality { get; set; }
}

public class Match
{
    public int Id { get; set; }
    public Competition Competition { get; set; }
    public Season Season { get; set; }
    public DateTime UtcDate { get; set; }
    public string Status { get; set; }
    public int Matchday { get; set; }
    public string Stage { get; set; }
    public object Group { get; set; }
    public DateTime LastUpdated { get; set; }
    public Odds Odds { get; set; }
    public Score Score { get; set; }
    public HomeTeam HomeTeam { get; set; }
    public AwayTeam AwayTeam { get; set; }
    public List<Referee> Referees { get; set; }
}