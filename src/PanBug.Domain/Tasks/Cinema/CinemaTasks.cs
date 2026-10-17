using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Cinema;

/// <summary>Zadanie 1: 12-latek nie dostaje biletu ulgowego (warunek "mniejszy niż" zamiast "do włącznie").</summary>
public sealed class Task01Cinema : CinemaSimulationBase
{
    public Task01Cinema(DateOnly? today = null) : base(1, PointValue.One, today) { }

    protected override bool IsReduced(int age) => age < ReducedMaxAge;
}

/// <summary>Zadanie 9: odznaczenie miejsca nie usuwa jego biletu, więc ponowne zaznaczenie liczy miejsce dwa razy.</summary>
public sealed class Task09Cinema : CinemaSimulationBase
{
    public Task09Cinema(DateOnly? today = null) : base(9, PointValue.Two, today) { }

    protected override void OnSeatDeselected(Seat seat) { }
}

/// <summary>Zadanie 18: seans po północy trafia na listę poprzedniego dnia ("doba kinowa" do 6:00).</summary>
public sealed class Task18Cinema : CinemaSimulationBase
{
    public Task18Cinema(DateOnly? today = null) : base(18, PointValue.Three, today) { }

    protected override DateOnly ListingDayOf(Showing showing) =>
        DateOnly.FromDateTime(showing.Start.AddHours(-6));
}
