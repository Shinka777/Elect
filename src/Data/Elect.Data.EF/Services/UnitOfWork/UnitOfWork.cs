﻿#region	License

//--------------------------------------------------
// <License>
//     <Copyright> 2018 © Top Nguyen </Copyright>
//     <Url> http://topnguyen.net/ </Url>
//     <Author> Top </Author>
//     <Project> Elect </Project>
//     <File>
//         <Name> UnitOfWork.cs </Name>
//         <Created> 25/03/2018 10:14:25 PM </Created>
//         <Key> 40b48b5c-6fe3-4783-b280-d2a915cc9431 </Key>
//     </File>
//     <Summary>
//         UnitOfWork.cs is a part of Elect
//     </Summary>
// <License>
//--------------------------------------------------

#endregion License

using System;
using System.Collections.Generic;
using Elect.Data.EF.Interfaces.DbContext;
using Elect.Data.EF.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elect.Data.EF.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elect.Data.EF.Services.UnitOfWork
{
    public abstract class UnitOfWork<TDbContext> : IUnitOfWork where TDbContext : IDbContext
    {
        public List<Func<IEnumerable<EntityEntry>, bool>> FunctionsBeforeSaveChanges { get; set; } = new List<Func<IEnumerable<EntityEntry>, bool>>();

        public List<Action<EntityStatesModel>> ActionsAfterSaveChanges { get; set; } = new List<Action<EntityStatesModel>>();

        public List<Action> ActionsBeforeCommit { get; set; } = new List<Action>();

        public List<Action> ActionsAfterCommit { get; set; } = new List<Action>();
        
        public List<Action> ActionsBeforeRollback { get; set; } = new List<Action>();
        
        public List<Action> ActionsAfterRollback { get; set; } = new List<Action>();
        
        protected readonly TDbContext DbContext;

        protected UnitOfWork(TDbContext dbContext)
        {
            DbContext = dbContext;
        }

        #region Transaction

        public virtual IUnitOfWorkTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            var transaction = new UnitOfWorkTransaction(DbContext.Database.BeginTransaction(isolationLevel))
            {
                ActionsBeforeCommit = ActionsBeforeCommit,
                ActionsAfterCommit = ActionsAfterCommit,
                ActionsBeforeRollback = ActionsBeforeRollback,
                ActionsAfterRollback = ActionsAfterRollback
            };

            return transaction;
        }

        public virtual IUnitOfWorkTransaction BeginTransaction()
        {
            var transaction = new UnitOfWorkTransaction(DbContext.Database.BeginTransaction())
            {
                ActionsBeforeCommit = ActionsBeforeCommit,
                ActionsAfterCommit = ActionsAfterCommit,
                ActionsBeforeRollback = ActionsBeforeRollback,
                ActionsAfterRollback = ActionsAfterRollback
            };

            return transaction;
        }

        public virtual async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = new UnitOfWorkTransaction(await DbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(true))
            {
                ActionsBeforeCommit = ActionsBeforeCommit,
                ActionsAfterCommit = ActionsAfterCommit,
                ActionsBeforeRollback = ActionsBeforeRollback,
                ActionsAfterRollback = ActionsAfterRollback
            };

            return transaction;
        }

        public virtual async Task<IUnitOfWorkTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            var transaction =  new UnitOfWorkTransaction(await DbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(true))
            {
                ActionsBeforeCommit = ActionsBeforeCommit,
                ActionsAfterCommit = ActionsAfterCommit,
                ActionsBeforeRollback = ActionsBeforeRollback,
                ActionsAfterRollback = ActionsAfterRollback
            };

            return transaction;
        }

        #endregion

        #region Save

        public virtual int SaveChanges()
        {
            bool isContinue = true;
                
            if (FunctionsBeforeSaveChanges?.Any() == true)
            {
                foreach (var func in FunctionsBeforeSaveChanges)
                {
                    var tempContinue = func?.Invoke(DbContext.ChangeTracker.Entries()) ?? true;

                    if (!tempContinue)
                    {
                        isContinue = false;
                    }
                }
            }
            
            var entityStatesModel =  SplitEntity();
            
            var result = isContinue ? DbContext.SaveChanges() : default;
            
            if (isContinue && ActionsAfterSaveChanges?.Any() == true)
            {
                foreach (var action in ActionsAfterSaveChanges)
                {
                    action?.Invoke(entityStatesModel);
                }
            }

            return result;
        }

        public virtual int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            bool isContinue = true;
                
            if (FunctionsBeforeSaveChanges?.Any() == true)
            {
                foreach (var func in FunctionsBeforeSaveChanges)
                {
                    var tempContinue = func?.Invoke(DbContext.ChangeTracker.Entries()) ?? true;

                    if (!tempContinue)
                    {
                        isContinue = false;
                    }
                }
            }
            
            var entityStatesModel =  SplitEntity();

            var result = isContinue ? DbContext.SaveChanges(acceptAllChangesOnSuccess) : default;
        
            if (isContinue && ActionsAfterSaveChanges?.Any() == true)
            {
                foreach (var action in ActionsAfterSaveChanges)
                {
                    action?.Invoke(entityStatesModel);
                }
            }

            return result;
        }

        public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            bool isContinue = true;
                
            if (FunctionsBeforeSaveChanges?.Any() == true)
            {
                foreach (var func in FunctionsBeforeSaveChanges)
                {
                    var tempContinue = func?.Invoke(DbContext.ChangeTracker.Entries()) ?? true;

                    if (!tempContinue)
                    {
                        isContinue = false;
                    }
                }
            }

            var entityStatesModel = SplitEntity();

            var result = isContinue ? DbContext.SaveChangesAsync(cancellationToken) : default;

            if (isContinue && ActionsAfterSaveChanges?.Any() == true)
            {
                foreach (var action in ActionsAfterSaveChanges)
                {
                    action?.Invoke(entityStatesModel);
                }
            }

            return result;
        }

        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            bool isContinue = true;
                
            if (FunctionsBeforeSaveChanges?.Any() == true)
            {
                foreach (var func in FunctionsBeforeSaveChanges)
                {
                    var tempContinue = func?.Invoke(DbContext.ChangeTracker.Entries()) ?? true;

                    if (!tempContinue)
                    {
                        isContinue = false;
                    }
                }
            }

            var entityStatesModel = SplitEntity();

            var result = isContinue ? DbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken) : default;

            if (isContinue && ActionsAfterSaveChanges?.Any() == true)
            {
                foreach (var action in ActionsAfterSaveChanges)
                {
                    action?.Invoke(entityStatesModel);
                }
            }

            return result;
        }

        #endregion
        
        protected virtual EntityStatesModel SplitEntity()
        {
            var listState = new List<EntityState>
            {
                EntityState.Added,
                EntityState.Modified,
                EntityState.Deleted
            };

            var listEntry = DbContext.ChangeTracker.Entries()
                .Where(x => listState.Contains(x.State))
                .ToList();

            EntityStatesModel entityStatesModel = new EntityStatesModel
            {
                ListAdded = listEntry.Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToList(),
                ListModified = listEntry.Where(x => x.State == EntityState.Modified).Select(x => x.Entity).ToList(),
                ListDeleted = listEntry.Where(x => x.State == EntityState.Deleted).Select(x => x.Entity).ToList()
            };

            return entityStatesModel;
        }

        #region SQL Command

        public DbCommand CreateCommand(string text, CommandType type = CommandType.Text, params SqlParameter[] parameters)
        {
            return DbContext.CreateCommand(text, type, parameters);
        }

        public void ExecuteCommand(string text, CommandType type = CommandType.Text, params SqlParameter[] parameters)
        {
            DbContext.ExecuteCommand(text, type, parameters);
        }

        public List<T> ExecuteCommand<T>(string text, CommandType type = CommandType.Text, params SqlParameter[] parameters) where T : class, new()
        {
            return DbContext.ExecuteCommand<T>(text, type, parameters);
        }

        #endregion
    }
}