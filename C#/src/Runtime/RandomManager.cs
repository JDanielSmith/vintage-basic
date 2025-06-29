namespace VintageBasic.Runtime;

sealed class RandomManager(BasicState state)
{
	readonly BasicState _state = state;

	public void SeedRandom(int seed)
	{
		_state.RandomGenerator = new Random(seed);
		// BASIC's RND often has a specific behavior for the first call after seeding,
		// or RND(-x) seeds and returns a value based on -x.
		// Here, we just reset the generator. The first call to GetRandomValue will use it.
		// We also need to ensure PreviousRandomValue is updated if BASIC expects RND(0) to use a value
		// influenced by the new seed immediately. However, RND(0) typically uses the *last generated* value.
		// For simplicity, we don't generate a new PreviousRandomValue here, it will be set by the next GetRandomValue call.
	}

	public void SeedRandomFromTime()
	{
		// Using TotalSeconds is a common way. Some BASICs might use milliseconds or other measures.
		int seed = (int)DateTime.Now.TimeOfDay.TotalSeconds;
		SeedRandom(seed);
	}

	public double PreviousRandomValue =>
		// In some BASICs, RND(0) returns the last number generated.
		_state.PreviousRandomValue;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
	public double GetRandomValue()
	{
		// System.Random.NextDouble() returns a value in [0.0, 1.0).
		// BASIC RND typically returns (0.0, 1.0) or [0.0, 1.0).
		// The exact range might matter for edge cases.
		double newValue = _state.RandomGenerator.NextDouble();
		_state.PreviousRandomValue = newValue;
		return newValue;
	}
}
