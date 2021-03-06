﻿using Autofac;
using Orchard.Data.MongoDb.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchard.Data.MongoDb
{
    public class MongoDbRepositoryBaseModule : Module
    {
        /// <summary>
        /// .EntityFramework session factory object.
        /// </summary>
        //private ISessionFactory _sessionFactory;
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(MongoDbRepositoryBase<>)).As(typeof(MongoDbRepositoryBase<>)).InstancePerDependency();
            builder.RegisterGeneric(typeof(MongoDbRepositoryBase<,>)).As(typeof(MongoDbRepositoryBase<,>)).InstancePerDependency();
        }
        protected override void AttachToComponentRegistration(Autofac.Core.IComponentRegistry componentRegistry, Autofac.Core.IComponentRegistration registration)
        {

        }

    }
}
