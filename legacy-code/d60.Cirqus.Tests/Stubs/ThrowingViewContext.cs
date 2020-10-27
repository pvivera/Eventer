﻿using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Stubs
{
    public class ThrowingViewContext : IViewContext
    {
        public ThrowingViewContext()
        {
            Items = new Dictionary<string, object>();
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        public void DeleteThisViewInstance()
        {
            throw new NotImplementedException("This view context is a stub that throws when someone uses it");
        }

        /// <summary>
        /// We do allow for setting/getting the event currently being handled though
        /// </summary>
        public DomainEvent CurrentEvent { get; set; }

        public Dictionary<string, object> Items { get; private set; }
    }
}