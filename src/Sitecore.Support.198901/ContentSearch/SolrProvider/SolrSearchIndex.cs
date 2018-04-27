using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Abstractions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Events;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Diagnostics;

namespace Sitecore.Support.ContentSearch.SolrProvider
{
  public class SolrSearchIndex : Sitecore.ContentSearch.SolrProvider.SolrSearchIndex
  {
    private readonly string name;

    public override string Name
    {
      get { return this.name; }
    }
    public virtual string Core { get; private set; }

    public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore, string group) :
      base(name, core, propertyStore, group)
    {
      Assert.ArgumentNotNull(name, "name");
      Assert.ArgumentNotNull(core, "core");
      Assert.ArgumentNotNull(propertyStore, "propertyStore");

      this.name = name;
      this.Core = core;
      this.PropertyStore = propertyStore;
    }

    public SolrSearchIndex(string name, string core, IIndexPropertyStore propertyStore) :
      this(name, core, propertyStore, null)
    {
    }

    public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds)
    {
      this.PerformUpdate(indexableUniqueIds, IndexingOptions.Default);
    }

    public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
    {
      this.PerformUpdate(indexableUniqueIds, indexingOptions);
    }

    private void PerformUpdate(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
    {
      if (!this.ShouldStartIndexing(indexingOptions))
        return;

      var events = this.Locator.GetInstance<IEvent>();
      var eventManager = this.Locator.GetInstance<IEventManager>();

      events.RaiseEvent("indexing:start", this.Name, false);
      eventManager.QueueEvent(new IndexingStartedEvent { IndexName = this.Name, FullRebuild = false });

      using (var context = this.CreateUpdateContext())
      {

        foreach (var uniqueId in indexableUniqueIds)
        {
          if (!this.ShouldStartIndexing(indexingOptions))
            return;


          foreach (var crawler in this.Crawlers)
          {
            crawler.Update(context, uniqueId, indexingOptions);
          }
        }

        context.Commit();
      }

      events.RaiseEvent("indexing:end", this.Name, false);
      eventManager.QueueEvent(new IndexingFinishedEvent { IndexName = this.Name, FullRebuild = false });
    }
  }
}