using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Cinema;

/// <summary>Zadanie 1: 12-latek nie dostaje biletu ulgowego (warunek "mniejszy niż" zamiast "do włącznie").</summary>
public sealed class Task01Cinema : CinemaSimulationBase
{
    public Task01Cinema(DateOnly? today = null) : base(1, PointValue.One, today) { }

    protected override bool IsReduced(int age) => age < ReducedMaxAge;
}

/// <summary>Zadanie 2: odznaczenie miejsca nie usuwa jego biletu, więc ponowne zaznaczenie liczy miejsce dwa razy.</summary>
public sealed class Task02Cinema : CinemaSimulationBase
{
    public Task02Cinema(DateOnly? today = null) : base(2, PointValue.Two, today) { }

    protected override void OnSeatDeselected(Seat seat) { }
}

/// <summary>Zadanie 3: seans po północy trafia na listę poprzedniego dnia ("doba kinowa" do 6:00).</summary>
public sealed class Task03Cinema : CinemaSimulationBase
{
    public Task03Cinema(DateOnly? today = null) : base(3, PointValue.Three, today) { }

    protected override DateOnly ListingDayOf(Showing showing) =>
        DateOnly.FromDateTime(showing.Start.AddHours(-6));
}
