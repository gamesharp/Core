namespace GameSharp.Collections.Tests.PooledList;

public sealed class PooledListOfReferenceTypeTests : Tests<PooledListOfReferenceTypeTests.Film>
{
    public sealed record Film(string Title, string Director, int Year);

    private static readonly Film[] _starWarsSeries =
    [
        new("Star Wars: Episode IV - A New Hope", "George Lucas", 1977),
        new("Star Wars: Episode V - The Empire Strikes Back", "Irvin Kershner", 1980),
        new("Star Wars: Episode VI - Return of the Jedi", "Richard Marquand", 1983),
        new("Star Wars: Episode I - The Phantom Menace", "George Lucas", 1999),
        new("Star Wars: Episode II - Attack of the Clones", "George Lucas", 2002),
        new("Star Wars: Episode III - Revenge of the Sith", "George Lucas", 2005),
        new("Star Wars: Episode VII - The Force Awakens", "J.J. Abrams", 2015),
        new("Star Wars: Episode VIII - The Last Jedi", "Rian Johnson", 2017),
        new("Star Wars: Episode IX - The Rise of Skywalker", "J.J. Abrams", 2019)
    ];
    protected override Film[] TestItems => _starWarsSeries;
}
