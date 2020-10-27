﻿using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Bugs.ReplicationScenario
{
    public class CountingRootIncremented : DomainEvent<CountingRoot>
    {
        public int NytTal { get; }

        public CountingRootIncremented(int nytTal)
        {
            NytTal = nytTal;
        }
    }
}