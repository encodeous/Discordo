namespace Discordo.Covid;

public class Case
{
    public string province { get; set; }
    public string date_report { get; set; }
    public DateOnly GetRealDate(){
        var spl = date_report.Split("-");
        return new DateOnly(int.Parse(spl[2]), int.Parse(spl[1]), int.Parse(spl[0]));
    }
    public int cases { get; set; }
    public int cumulative_cases { get; set; }
}

public class Mortality
{
    public string province { get; set; }
    public string date_death_report { get; set; }
    public int deaths { get; set; }
    public int cumulative_deaths { get; set; }
}

public class Recovered
{
    public string province { get; set; }
    public string date_recovered { get; set; }
    public int recovered { get; set; }
    public int cumulative_recovered { get; set; }
}

public class Testing
{
    public string province { get; set; }
    public string date_testing { get; set; }
    public int testing { get; set; }
    public int cumulative_testing { get; set; }
    public string testing_info { get; set; }
}

public class Active
{
    public string province { get; set; }
    public string date_active { get; set; }
    public int cumulative_cases { get; set; }
    public int cumulative_recovered { get; set; }
    public int cumulative_deaths { get; set; }
    public int active_cases { get; set; }
    public int active_cases_change { get; set; }
}

public class CovidStat
{
    public List<Case> cases { get; set; }
    public List<Mortality> mortality { get; set; }
    public List<Recovered> recovered { get; set; }
    public List<Testing> testing { get; set; }
    public List<Active> active { get; set; }
    public List<object> avaccine { get; set; }
    public List<object> dvaccine { get; set; }
    public List<object> cvaccine { get; set; }
    public string deprecation_warning { get; set; }
}