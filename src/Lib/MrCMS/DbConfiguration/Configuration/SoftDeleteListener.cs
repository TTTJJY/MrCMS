﻿using System;
using System.Collections.Generic;
using MrCMS.DbConfiguration.Helpers;
using MrCMS.Entities;
using MrCMS.Helpers;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Persister.Entity;

namespace MrCMS.DbConfiguration.Configuration
{
    [Serializable]
    public class SoftDeleteListener : DefaultDeleteEventListener
    {
        protected override void DeleteEntity(IEventSource session, object entity, EntityEntry entityEntry,
            bool isCascadeDeleteEnabled, IEntityPersister persister, ISet<object> transientEntities)
        {
            var context = ((ISession)session).GetContext();
            if (entity is SystemEntity systemEntity && !(context != null && context.IsSoftDeleteDisabled()))
            {
                systemEntity.IsDeleted = true;

                CascadeBeforeDelete(session, persister, systemEntity, entityEntry, transientEntities);
                CascadeAfterDelete(session, persister, systemEntity, transientEntities);
            }
            else
            {
                base.DeleteEntity(session, entity, entityEntry, isCascadeDeleteEnabled,
                    persister, transientEntities);
            }
        }
    }
}