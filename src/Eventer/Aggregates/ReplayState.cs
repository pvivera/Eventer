namespace Eventer.Aggregates
{
    internal enum ReplayState
    {
        None,
        EmitApply,
        ReplayApply,
    }
}