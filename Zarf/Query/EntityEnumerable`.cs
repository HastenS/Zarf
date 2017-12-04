﻿using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zarf.Core;

namespace Zarf.Query
{
    public class EntityEnumerable<TEntity> : IEnumerable<TEntity>
    {
        protected IEnumerator<TEntity> Enumerator { get; set; }

        protected Expression Expression { get; }

        protected IQueryExecutor Interpreter { get; }

        public EntityEnumerable(Expression query, IDbContextParts dbContextParts)
        {
            Expression = query;
            Interpreter = new QueryExecutor(dbContextParts);
        }

        public virtual IEnumerator<TEntity> GetEnumerator()
        {
            return Enumerator ?? (Enumerator = Interpreter.Execute<TEntity>(Expression));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TEntity>)this).GetEnumerator();
        }
    }
}
