namespace SharpEngine;

public readonly record struct GameTime(TimeSpan Elapsed, TimeSpan Total, long Frame);
